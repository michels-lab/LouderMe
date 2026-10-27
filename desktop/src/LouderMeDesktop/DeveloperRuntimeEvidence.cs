using System.Text;
using System.Text.Json;

namespace LouderMeDesktop;

// Developer-only, metadata-only runtime fault census. This deliberately
// cannot store exception messages, paths, device IDs, credentials or stacks.
internal sealed record DeveloperRuntimeIncident(
    string Code, DateTimeOffset FirstUtc, DateTimeOffset LastUtc, int Occurrences);

internal static class DeveloperRuntimeEvidence
{
    private const int MaxBytes = 16_384;
    private const int MaxOccurrences = 100_000;
    private const int MaxCodes = 16;
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "APP_STARTUP_EXCEPTION",
        "DEVELOPER_NOTES_SAVE_FAILED",
        "DEVELOPER_REPORT_SAVE_FAILED",
    };
    internal static string EvidencePath =>
        Path.Combine(DeveloperMode.DataDirectory, "runtime-incidents-v1.json");

    internal static List<DeveloperRuntimeIncident> Read()
    {
        EnsureDeveloper();
        if (!File.Exists(EvidencePath)) return new();
        if (new FileInfo(EvidencePath).Length > MaxBytes)
            throw new InvalidDataException("Developer runtime metadata is too large.");
        try
        {
            var data = JsonSerializer.Deserialize<List<DeveloperRuntimeIncident>>(
                File.ReadAllText(EvidencePath, Encoding.UTF8));
            if (data is null) throw new InvalidDataException("Runtime evidence was null.");
            Validate(data);
            return data;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Developer runtime metadata cannot be parsed.", ex);
        }
    }

    // Always called from an already-detected fault path; never changes the
    // original app exception handling or audio behavior.
    internal static void TryRecord(string code)
    {
        if (!DeveloperMode.Enabled || !Allowed.Contains(code)) return;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var notes = Read();
            var index = notes.FindIndex(n => n.Code == code);
            if (index >= 0)
            {
                var before = notes[index];
                notes[index] = before with
                {
                    LastUtc = now,
                    Occurrences = Math.Min(MaxOccurrences, before.Occurrences + 1)
                };
            }
            else notes.Add(new DeveloperRuntimeIncident(code, now, now, 1));
            Validate(notes);
            Directory.CreateDirectory(DeveloperMode.DataDirectory);
            var newText = JsonSerializer.Serialize(notes);
            if (Encoding.UTF8.GetByteCount(newText) > MaxBytes)
                throw new InvalidDataException("Runtime metadata exceeded its bound.");
            var temp = EvidencePath + ".tmp";
            try
            {
                File.WriteAllText(temp, newText, new UTF8Encoding(false));
                File.Move(temp, EvidencePath, overwrite: true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        catch (Exception)
        {
            // Never mask the true exception. Corrupt evidence is never
            // overwritten; Developer RunAudit reports a separate problem.
        }
    }

    internal static List<DeveloperFinding> Diagnose()
    {
        if (!DeveloperMode.Enabled) return new();
        try
        {
            return Read().Select(n => new DeveloperFinding(
                n.Code, "P1", "runtime",
                $"Observed {n.Occurrences} fault event(s); first UTC {n.FirstUtc:O}, last UTC {n.LastUtc:O}.",
                "Developer-only local closed-code event history; not a stack trace or physical audio test"))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return new()
            {
                new("RUNTIME_HISTORY_UNREADABLE", "P1", "runtime",
                    "Developer fault history could not be verified; original data preserved.",
                    "read-only file integrity probe; no raw diagnostic values exported")
            };
        }
    }

    private static void EnsureDeveloper()
    {
        if (!DeveloperMode.Enabled)
            throw new InvalidOperationException("Public LouderMe must never access Developer diagnostics.");
    }

    private static void Validate(IReadOnlyList<DeveloperRuntimeIncident> events)
    {
        if (events.Count > MaxCodes || events.Select(n => n.Code).Distinct(StringComparer.Ordinal).Count() != events.Count ||
            events.Any(n => n is null || !Allowed.Contains(n.Code) ||
                n.FirstUtc == default || n.LastUtc < n.FirstUtc || n.FirstUtc.Offset != TimeSpan.Zero ||
                n.LastUtc.Offset != TimeSpan.Zero || n.Occurrences < 1 ||
                n.Occurrences > MaxOccurrences))
            throw new InvalidDataException("Developer runtime history has invalid metadata.");
    }

    internal static void RunContractSelfTests()
    {
        EnsureDeveloper();
        TryRecord("APP_STARTUP_EXCEPTION");
        TryRecord("APP_STARTUP_EXCEPTION");
        var incidents = Read();
        if (incidents.Count != 1 || incidents[0].Occurrences != 2 ||
            Diagnose().Single().Code != "APP_STARTUP_EXCEPTION")
            throw new InvalidOperationException("Developer runtime fault history did not persist correctly.");
        TryRecord("private-user-path-or-unapproved-message");
        if (Read().Count != 1 || Read()[0].Occurrences != 2)
            throw new InvalidOperationException("Unapproved free-text runtime message was accepted.");
        var original = File.ReadAllText(EvidencePath);
        File.WriteAllText(EvidencePath, "{corrupt");
        if (Diagnose().Single().Code != "RUNTIME_HISTORY_UNREADABLE")
            throw new InvalidOperationException("Corrupted diagnostic history was mislabeled healthy.");
        TryRecord("DEVELOPER_NOTES_SAVE_FAILED");
        if (File.ReadAllText(EvidencePath) != "{corrupt")
            throw new InvalidOperationException("Unreadable original error history was overwritten.");
        File.WriteAllText(EvidencePath, original);
        var eventList = Read();
        eventList[0] = eventList[0] with { Code = "EXCEPTION_MESSAGE_SENSITIVE" };
        File.WriteAllText(EvidencePath, JsonSerializer.Serialize(eventList));
        if (Diagnose().Single().Code != "RUNTIME_HISTORY_UNREADABLE")
            throw new InvalidOperationException("Unapproved diagnostic code was accepted.");
        File.WriteAllText(EvidencePath, original);
        if (Read()[0].Occurrences != 2)
            throw new InvalidOperationException("Clean diagnostic evidence could not be recovered.");
        Console.WriteLine("PASS: privacy-safe Developer runtime audit, persisted fault counts and corrupt-history recovery.");
    }
}
