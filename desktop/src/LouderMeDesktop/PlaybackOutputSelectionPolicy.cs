namespace LouderMeDesktop;

/// <summary>
/// Choosing an endpoint here changes only LouderMe's target for Windows
/// 0–100% endpoint volume/mute. It never reroutes the OS default output,
/// installs an APO, or proves that Boost is audible on that output.
/// </summary>
internal sealed record PlaybackOutputChoice(string? Id, string Name, bool Available = true)
{
    public override string ToString() => Name;
}

internal static class PlaybackOutputSelectionPolicy
{
    internal static IReadOnlyList<PlaybackOutputChoice> Build(
        IReadOnlyList<PlaybackOutputChoice> active, string? selectedId)
    {
        ArgumentNullException.ThrowIfNull(active);
        var choices = new List<PlaybackOutputChoice>
        {
            new(null, "Follow Windows default output")
        };
        foreach (var item in active.Where(x => !string.IsNullOrWhiteSpace(x.Id))
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.Id, StringComparer.Ordinal))
            choices.Add(item with { Available = true });

        // A missing preferred device stays selected but is never live. Never
        // switch to another USB/Bluetooth/HDMI endpoint automatically.
        if (!string.IsNullOrWhiteSpace(selectedId) &&
            !choices.Any(x => string.Equals(x.Id, selectedId, StringComparison.Ordinal)))
            choices.Add(new(selectedId,
                "Previously chosen output unavailable — select another", false));
        return choices;
    }

    internal static int SelectedIndex(
        IReadOnlyList<PlaybackOutputChoice> choices, string? selectedId)
    {
        int result = -1;
        for (int i = 0; i < choices.Count; i++)
        {
            if (!string.Equals(choices[i].Id, selectedId, StringComparison.Ordinal))
                continue;
            if (result != -1) return -1; // never select an ambiguous ID
            result = i;
        }
        return result;
    }

    internal static bool SameChoices(
        IReadOnlyList<PlaybackOutputChoice> a, IReadOnlyList<PlaybackOutputChoice> b) =>
        a.Count == b.Count && a.Zip(b).All(pair => pair.First == pair.Second);

    internal static void RunSelfTests()
    {
        PlaybackOutputChoice[] active =
        [
            new("speaker", "Speakers"),
            new("headset", "Bluetooth Headset"),
            new("hdmi", "HDMI")
        ];
        var available = Build(active, "headset");
        if (available.Count != 4 ||
            SelectedIndex(available, "headset") != 1 ||
            SelectedIndex(available, null) != 0 ||
            !available[1].Available ||
            !SameChoices(available, Build(active, "headset")))
            throw new InvalidOperationException("Available selected output was not preserved.");

        var missing = Build([active[0], active[2]], "headset");
        if (missing.Count != 4 ||
            SelectedIndex(missing, "headset") != 3 ||
            missing[3].Available ||
            !missing[3].Name.Contains("unavailable", StringComparison.Ordinal) ||
            SelectedIndex(missing, null) != 0 ||
            SameChoices(available, missing))
            throw new InvalidOperationException("Unplugged endpoint silently fell back to a different speaker.");

        var returned = Build(active, "headset");
        if (SelectedIndex(returned, "headset") != 1 ||
            SelectedIndex(Build(active, "speaker"), "speaker") != 3 ||
            SelectedIndex([active[0], active[0]], "speaker") != -1)
            throw new InvalidOperationException("Playback target A-B-A or ambiguous endpoint regression.");

        Console.WriteLine(
            "PASS: Windows default/manual output volume selection, missing-device pinning, " +
            "same-ID reconnect, no auto-reroute and duplicate-ID rejection.");
    }
}
