using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouderMeDesktop;

// Developer and public editions share the actual UI/audio engine but NEVER their
// app settings, diagnostics, annotations, mutex or persistent developer data.
internal static class DeveloperMode
{
    internal static bool Enabled { get; private set; }
    internal static string? ContractTestDirectory { get; set; }
    internal static void Configure(bool enabled) => Enabled = enabled;

    internal static string DataDirectory => ContractTestDirectory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Michel's Lab", Enabled ? "LouderMe.Developer" : "LouderMe");
}

internal sealed record DeveloperAnnotation(
    Guid Id, string Screen, string Locator, string Label, string Comment,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    string Severity, string Status);

internal sealed record DeveloperFinding(
    string Code, string Severity, string Category, string Observation, string Evidence);

internal sealed record DeveloperAuditReport(
    string Product, string Edition, string Platform, string Version, DateTimeOffset AuditedAtUtc,
    string EvidenceLevel, List<DeveloperFinding> Findings, List<DeveloperAnnotation> Annotations);

internal static class DeveloperAudit
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    internal static string NotesPath => Path.Combine(DeveloperMode.DataDirectory, "annotations.json");

    internal static List<DeveloperAnnotation> LoadNotes()
    {
        if (!File.Exists(NotesPath)) return new();
        // Corrupt data must be reported, not silently replaced by a blank set.
        try
        {
            return JsonSerializer.Deserialize<List<DeveloperAnnotation>>(File.ReadAllText(NotesPath), Json)
                ?? throw new InvalidDataException("Annotation file contains null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Developer annotation JSON cannot be read; the original was preserved.", ex);
        }
    }

    internal static void SaveNotes(IReadOnlyList<DeveloperAnnotation> notes)
    {
        if (!DeveloperMode.Enabled)
            throw new InvalidOperationException("Developer annotations cannot be saved from the public edition.");
        Directory.CreateDirectory(DeveloperMode.DataDirectory);
        var temp = NotesPath + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(notes, Json), Encoding.UTF8);
            File.Move(temp, NotesPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    internal static string Locator(Control control)
    {
        var parts = new Stack<string>();
        for (Control? current = control; current is not null; current = current.Parent)
        {
            var identity = string.IsNullOrWhiteSpace(current.Name)
                ? "[" + (current.Parent?.Controls.GetChildIndex(current) ?? 0) + "]"
                : "#" + Uri.EscapeDataString(current.Name);
            parts.Push(current.GetType().Name + identity);
        }
        return string.Join("/", parts);
    }

    internal static bool Exists(string locator, IEnumerable<Form> openForms)
    {
        foreach (var form in openForms)
        {
            if (Walk(form).Any(control => string.Equals(Locator(control), locator, StringComparison.Ordinal)))
                return true;
        }
        return false;
    }

    internal static IEnumerable<Control> Walk(Control control)
    {
        yield return control;
        foreach (Control child in control.Controls)
        foreach (var nested in Walk(child))
            yield return nested;
    }

    internal static DeveloperAuditReport Run(Form application, IReadOnlyList<DeveloperAnnotation> notes)
    {
        if (!DeveloperMode.Enabled)
            throw new InvalidOperationException("The public application cannot run developer audits.");
        var findings = new List<DeveloperFinding>();
        var allControls = Walk(application).ToArray();
        if (!application.IsHandleCreated)
            findings.Add(new("UI_WINDOW_NOT_READY", "P1", "runtime", "Main UI handle is not ready.", "local runtime"));
        if (!allControls.Any(c => c is TrackBar))
            findings.Add(new("UI_VOLUME_SLIDER_MISSING", "P1", "controls", "No actual slider exists in the running UI.", "live WinForms tree"));
        if (allControls.Count(c => c is NumericUpDown) != 7)
            findings.Add(new("UI_EQ_BANDS_INCOMPLETE", "P1", "controls", "Expected seven live equalizer band controls.", "live WinForms tree"));

        var about = allControls.OfType<Button>().FirstOrDefault(b => b.Text == "About");
        if (about is null || !about.Visible || !about.Enabled || about.Width <= 0)
            findings.Add(new("UI_ABOUT_NOT_VISIBLE", "P1", "navigation", "Persistent About control is missing or unavailable.", "live WinForms tree"));

        // Report the APC configuration honestly: disk config is NOT audible playback evidence.
        try
        {
            var apo = new EqualizerApoEngine().GetStatus();
            if (!apo.Installed)
                findings.Add(new("APO_NOT_INSTALLED", "P1", "audio", "Equalizer APO is absent; Windows system-wide Boost/EQ cannot be confirmed.", "read-only installation check"));
            else if (!apo.Configured)
                findings.Add(new("APO_NOT_LINKED", "P1", "audio", "LouderMe's Equalizer APO include is not active.", "read-only config check"));
            else
                findings.Add(new("AUDIO_AUDIBILITY_UNVERIFIED", "P2", "audio", "Disk-level APO presence never proves audible processing on the current Windows endpoint.", "configuration only"));
        }
        catch (Exception)
        {
            findings.Add(new("APO_DIAGNOSTIC_UNAVAILABLE", "P1", "audio", "Could not complete read-only APO configuration check.", "caught exception type only; no paths or stack trace exported"));
        }

        var forms = Application.OpenForms.Cast<Form>().Where(f => f is MainForm || f is AboutForm).ToArray();
        foreach (var note in notes.Where(n => n.Status != "Resolved"))
        {
            // Closed screens are merely not mounted. Only label a locator stale
            // when its owning screen is actually open for a meaningful probe.
            var screenMounted = note.Screen == "Home" ? forms.Any(f => f is MainForm)
                : note.Screen == "About" ? forms.Any(f => f is AboutForm)
                : false;
            if (screenMounted && !Exists(note.Locator, forms))
                findings.Add(new("ANNOTATION_LOCATOR_UNRESOLVED", "P2", "annotations",
                    "A saved owner note does not match any currently open surface; do not reattach automatically.",
                    "anchor " + note.Id.ToString("D")));
        }
        return new DeveloperAuditReport("LouderMe", "Developer Edition", "Windows",
            Application.ProductVersion, DateTimeOffset.UtcNow, "local read-only runtime/config checks; no audible hardware proof",
            findings, notes.ToList());
    }

    internal static string JsonReport(DeveloperAuditReport report) => JsonSerializer.Serialize(report, Json);

    internal static string AiRepairPrompt(DeveloperAuditReport report)
    {
        var output = new StringBuilder();
        output.AppendLine("# LouderMe Developer Edition — AI repair handoff")
            .AppendLine($"Platform: {report.Platform}; Version: {report.Version}; Captured UTC: {report.AuditedAtUtc:O}")
            .AppendLine($"Evidence: {report.EvidenceLevel}")
            .AppendLine("Do not infer causes or claim that audio was measured. Preserve public UI, files, update signer and user state.")
            .AppendLine()
            .AppendLine("## Read-only audit findings");
        foreach (var f in report.Findings)
            output.AppendLine($"- [{f.Severity}] {f.Code} ({f.Category}): {f.Observation} [{f.Evidence}]");
        output.AppendLine().AppendLine("## Owner-reported observations (NOT automatically verified)");
        foreach (var note in report.Annotations)
        {
            output.AppendLine($"- Note {note.Id:D}: {note.Status} / {note.Severity}")
                .AppendLine($"  Screen: {note.Screen}")
                .AppendLine($"  Exact target: {note.Locator}")
                .AppendLine($"  Label: {note.Label}")
                .AppendLine($"  Created UTC: {note.CreatedAtUtc:O}; Updated UTC: {note.UpdatedAtUtc:O}")
                .AppendLine("  Verbatim owner comment:")
                .AppendLine(note.Comment);
        }
        output.AppendLine().AppendLine("## Requested implementation")
            .AppendLine("Reproduce each finding/owner observation; distinguish unverified reports from runtime evidence; add regression coverage and explain limits. Never include credentials or private input values.");
        return output.ToString();
    }

    // Runs as a real .NET executable contract test in CI without loading user or audio settings.
    internal static void RunContractSelfTests()
    {
        DeveloperMode.Configure(true);
        var publicPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Michel's Lab", "LouderMe", "desktop-settings.json");
        if (DesktopSettingsStore.SettingsPath == publicPath)
            throw new InvalidOperationException("Developer and public settings paths collide.");
        var isolatedTest = Path.Combine(Path.GetTempPath(), "LouderMe-DevAudit-" + Guid.NewGuid().ToString("N"));
        DeveloperMode.ContractTestDirectory = isolatedTest;
        var note = new DeveloperAnnotation(Guid.NewGuid(), "Home", "MainForm[0]/TrackBar#volume",
            "Output level", "owner note preserved verbatim\nand across exports", DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow, "P1", "Open");
        var report = new DeveloperAuditReport("LouderMe", "Developer Edition", "Windows",
            "contract-test", DateTimeOffset.UtcNow, "fixture", new()
            {
                new("TEST_CODE", "P1", "test", "No secret or stack value", "synthetic fixture")
            }, new() { note });
        var json = JsonReport(report);
        var prompt = AiRepairPrompt(report);
        if (!json.Contains(note.Locator, StringComparison.Ordinal) ||
            !json.Contains(note.Comment.Replace("\n", "\\n", StringComparison.Ordinal), StringComparison.Ordinal) ||
            !prompt.Contains(note.Comment, StringComparison.Ordinal) ||
            !prompt.Contains("NOT automatically verified", StringComparison.Ordinal))
            throw new InvalidOperationException("Developer annotation export contract failed.");
        if (json.Contains(publicPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Private production file path exported.");
        try
        {
            SaveNotes(new[] { note });
            var loaded = LoadNotes();
            if (loaded.Count != 1 || loaded[0].Id != note.Id || loaded[0].Comment != note.Comment)
                throw new InvalidOperationException("Annotations were not restored exactly from disk.");
            File.WriteAllText(NotesPath, "{corrupted");
            try
            {
                LoadNotes();
                throw new InvalidOperationException("Corrupt annotation JSON was incorrectly accepted.");
            }
            catch (InvalidDataException)
            {
                if (File.ReadAllText(NotesPath) != "{corrupted")
                    throw new InvalidOperationException("Corrupt annotation evidence was overwritten.");
            }
        }
        finally
        {
            DeveloperMode.ContractTestDirectory = null;
            if (Directory.Exists(isolatedTest)) Directory.Delete(isolatedTest, recursive: true);
        }
    }
}
