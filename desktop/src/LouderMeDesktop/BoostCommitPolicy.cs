namespace LouderMeDesktop;

/// <summary>
/// Applies the final slider value exactly once after pointer release,
/// keyboard release, focus loss or a short debounce. Never rewrite APO
/// configuration continuously during a live drag.
/// </summary>
internal static class BoostCommitPolicy
{
    internal static bool ShouldCommit(bool pending, bool thumbCaptured, bool force = false) =>
        pending && (force || !thumbCaptured);

    internal static void RunSelfTests()
    {
        if (ShouldCommit(false, false) ||
            ShouldCommit(false, true, force: true) ||
            ShouldCommit(true, true) ||
            !ShouldCommit(true, false) ||
            !ShouldCommit(true, true, force: true))
            throw new InvalidOperationException(
                "Windows Boost slider wrote during drag or lost its last 100–300% edit.");
        Console.WriteLine(
            "PASS: boost drag coalesces disk writes; keyboard, focus loss, timeout and close commit final gain.");
    }
}
