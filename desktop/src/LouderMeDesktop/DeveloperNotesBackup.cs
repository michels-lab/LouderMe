using System.Text;
using System.Text.Json;

namespace LouderMeDesktop;

// Device-free, Windows Developer-only, owner-authorized note migration.
// Public LouderMe settings, account data, and sound devices are never read.
internal sealed record DeveloperNoteBackup(
    string Schema,
    string Product,
    string Edition,
    string Platform,
    DateTimeOffset ExportedAtUtc,
    List<DeveloperAnnotation> OwnerAnnotations);

internal sealed record DeveloperNoteImportPreview(
    List<DeveloperAnnotation> Incoming, int NewCount, int ExactDuplicates,
    int Conflicts);

internal static class DeveloperNotesBackup
{
    internal const int MaxBytes = 2_097_152;
    internal const int MaxNotes = 250;
    internal const string Schema = "michelslab-developer-note-backup-v1";

    private static readonly JsonSerializerOptions Format = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        MaxDepth = 16,
    };

    internal static string Export(IReadOnlyList<DeveloperAnnotation> notes)
    {
        EnsureDeveloper();
        Validate(notes);
        var payload = JsonSerializer.Serialize(new DeveloperNoteBackup(
            Schema, "LouderMe", "Developer Edition", "Windows", DateTimeOffset.UtcNow,
            notes.ToList()), Format);
        if (Encoding.UTF8.GetByteCount(payload) > MaxBytes)
            throw new InvalidDataException("Developer notes exceed the 2 MB backup limit.");
        return payload;
    }

    internal static DeveloperNoteImportPreview Preview(
        string payload, IReadOnlyList<DeveloperAnnotation> existing)
    {
        EnsureDeveloper();
        if (Encoding.UTF8.GetByteCount(payload) > MaxBytes)
            throw new InvalidDataException("Selected backup exceeds the 2 MB limit.");
        DeveloperNoteBackup backup;
        try
        {
            backup = JsonSerializer.Deserialize<DeveloperNoteBackup>(payload, Format)
                ?? throw new InvalidDataException("Developer backup JSON was null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Developer backup JSON cannot be read.", ex);
        }
        if (backup.Schema != Schema ||
            backup.Product != "LouderMe" || backup.Edition != "Developer Edition" ||
            backup.Platform != "Windows" || backup.ExportedAtUtc == default ||
            backup.ExportedAtUtc.Offset != TimeSpan.Zero ||
            backup.OwnerAnnotations is null)
            throw new InvalidDataException("Backup must originate from the LouderMe Windows Developer Edition.");
        Validate(backup.OwnerAnnotations);
        Validate(existing);
        var current = existing.ToDictionary(n => n.Id);
        var incoming = backup.OwnerAnnotations;
        return new DeveloperNoteImportPreview(
            incoming,
            incoming.Count(n => !current.ContainsKey(n.Id)),
            incoming.Count(n => current.TryGetValue(n.Id, out var old) && old == n),
            incoming.Count(n => current.TryGetValue(n.Id, out var old) && old != n));
    }

    internal static List<DeveloperAnnotation> Merge(
        IReadOnlyList<DeveloperAnnotation> existing, DeveloperNoteImportPreview preview,
        bool replaceConflicts)
    {
        EnsureDeveloper();
        Validate(existing);
        Validate(preview.Incoming);
        var byId = preview.Incoming.ToDictionary(n => n.Id);
        var oldIds = existing.Select(n => n.Id).ToHashSet();
        var combined = existing.Select(n =>
            replaceConflicts && byId.TryGetValue(n.Id, out var replacement)
                ? replacement : n).ToList();
        combined.AddRange(preview.Incoming.Where(n => !oldIds.Contains(n.Id)));
        Validate(combined);
        return combined; // caller MUST persist before replacing in-memory notes
    }

    private static void EnsureDeveloper()
    {
        if (!DeveloperMode.Enabled)
            throw new InvalidOperationException("Developer backups are unavailable in the public app.");
    }

    internal static void Validate(IReadOnlyList<DeveloperAnnotation> notes)
    {
        if (notes.Count > MaxNotes)
            throw new InvalidDataException("Backups can contain no more than 250 notes.");
        var ids = new HashSet<Guid>();
        foreach (var n in notes)
        {
            if (n is null || n.Id == Guid.Empty || !ids.Add(n.Id) ||
                n.Screen is not ("Home" or "About") ||
                string.IsNullOrWhiteSpace(n.Locator) || n.Locator.Length > 1024 ||
                string.IsNullOrWhiteSpace(n.Label) || n.Label.Length > 200 ||
                string.IsNullOrWhiteSpace(n.Comment) || n.Comment.Length > 12000 ||
                n.Severity is not ("P0" or "P1" or "P2") ||
                n.Status is not ("Open" or "Investigating" or "Resolved") ||
                n.CreatedAtUtc == default || n.UpdatedAtUtc == default ||
                n.CreatedAtUtc.Offset != TimeSpan.Zero || n.UpdatedAtUtc.Offset != TimeSpan.Zero ||
                n.UpdatedAtUtc < n.CreatedAtUtc)
                throw new InvalidDataException("Invalid, duplicate or out-of-scope Developer annotation.");
        }
    }

    // Actual pure migration contract run by --check-developer-contract on CI.
    internal static void RunContractSelfTests()
    {
        EnsureDeveloper();
        var created = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
        var first = new DeveloperAnnotation(Guid.NewGuid(), "Home",
            "MainForm#Home/TrackBar#Volume", "Volume",
            "Exact multiline owner text\n\"quoted\" and Unicode á", created, created,
            "P1", "Investigating");
        var second = new DeveloperAnnotation(Guid.NewGuid(), "About",
            "AboutForm#About/Label#Brand", "Brand",
            "The approved Michel's Lab logo must remain", created, created.AddMinutes(1),
            "P2", "Open");
        var original = new List<DeveloperAnnotation> { first };
        var exported = Export(new[] { first, second });
        var preview = Preview(exported, original);
        if (preview.NewCount != 1 || preview.ExactDuplicates != 1 ||
            preview.Conflicts != 0 || preview.Incoming[0] != first ||
            preview.Incoming[1] != second ||
            Merge(original, preview, false).Count != 2 ||
            Merge(original, preview, false)[0].Comment != first.Comment)
            throw new InvalidOperationException("Developer backup failed exact UTF-8 or non-destructive merge.");

        var revised = first with { Comment = "revised only with owner's explicit consent",
            UpdatedAtUtc = created.AddMinutes(3) };
        var conflicting = Preview(Export(new[] { revised, second }), original);
        if (conflicting.Conflicts != 1 || conflicting.NewCount != 1 ||
            Merge(original, conflicting, false)[0] != first ||
            Merge(original, conflicting, true)[0] != revised ||
            original[0] != first)
            throw new InvalidOperationException("Developer restore silently overwrote an existing note.");

        static void Reject(string label, Action action)
        {
            try { action(); }
            catch (InvalidDataException) { return; }
            throw new InvalidOperationException("Unsafe Developer backup was accepted: " + label);
        }
        Reject("other product", () => Preview(exported.Replace(
            "\"product\": \"LouderMe\"", "\"product\": \"Other\""), original));
        Reject("other platform", () => Preview(exported.Replace(
            "\"platform\": \"Windows\"", "\"platform\": \"Android\""), original));
        Reject("public edition", () => Preview(exported.Replace(
            "\"edition\": \"Developer Edition\"", "\"edition\": \"Public\""), original));
        Reject("missing JSON", () => Preview("{", original));
        Reject("oversized JSON", () => Preview(new string('x', MaxBytes + 1), original));
        Reject("duplicate ID", () => Preview(Export(new[] { first, second }).Replace(
            second.Id.ToString("D"), first.Id.ToString("D")), original));
        Reject("invalid priority", () => Export(new[] { first with { Severity = "P9" } }));
        Reject("invalid timestamp", () => Export(new[] {
            first with { UpdatedAtUtc = first.CreatedAtUtc.AddSeconds(-1) } }));
        Reject("invalid screen", () => Export(new[] { first with { Screen = "Settings" } }));
        Reject("empty comment", () => Export(new[] { first with { Comment = " " } }));
        Reject("missing origin ID", () => Export(new[] { first with { Id = Guid.Empty } }));
        Reject("invalid status", () => Export(new[] { first with { Status = "Unknown" } }));
        Reject("too many records", () => Export(Enumerable.Range(0, MaxNotes + 1)
            .Select(_ => first with { Id = Guid.NewGuid() }).ToList()));
        // The same real file guards must refuse public-mode access without
        // changing the current Developer profile or opening user data.
        DeveloperMode.Configure(false);
        try
        {
            try
            {
                Export(new[] { first });
                throw new InvalidOperationException("Public build may not export private Developer notes.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("unavailable in the public app", StringComparison.Ordinal)) { }
        }
        finally { DeveloperMode.Configure(true); }
        if (Preview(Export(Array.Empty<DeveloperAnnotation>()), original).Incoming.Count != 0)
            throw new InvalidOperationException("Empty Developer backup must import nothing.");
        Console.WriteLine("PASS: Windows Developer backup UTF-8, explicit conflict resolution, 13 adversarial controls, no personal files.");
    }
}
