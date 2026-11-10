namespace LouderMeDesktop;

/// <summary>
/// UI-only write coalescing. A curve drag must stay responsive and write the
/// final seven-band state once when the user releases the pointer. Keyboard
/// edits can settle on the WinForms timer or on focus loss. Closing the form
/// always flushes a pending edit even if the pointer is still captured.
/// This policy never certifies audible DSP processing.
/// </summary>
internal static class EqualizerCommitPolicy
{
    internal static bool ShouldCommit(bool pending, bool curveDragging, bool force = false) =>
        pending && (force || !curveDragging);

    internal static void RunSelfTests()
    {
        if (ShouldCommit(false, false) ||
            ShouldCommit(false, true, force: true) ||
            ShouldCommit(true, true) ||
            !ShouldCommit(true, false) ||
            !ShouldCommit(true, true, force: true))
            throw new InvalidOperationException(
                "EQ apply coalescing wrote during mouse drag or dropped a final edit.");
        Console.WriteLine(
            "PASS: EQ drag defers APO writes; release, numeric focus loss and form close can flush.");
    }
}
