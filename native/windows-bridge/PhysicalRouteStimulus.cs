namespace LouderMeAudioBridge;

/// <summary>
/// Device-independent setup contract for the two mutually exclusive physical
/// route evidence sources. The independent Windows app MUST NOT instantiate
/// or initialize the bridge's own tone, which is null in external-app mode.
/// </summary>
internal static class PhysicalRouteStimulus
{
    internal static void Initialize(
        bool ownTone, bool externalAppTone, Action initializeOwnTone)
    {
        ArgumentNullException.ThrowIfNull(initializeOwnTone);
        if (ownTone == externalAppTone)
            throw new ArgumentException(
                "Exactly one physical-route stimulus mode is required.");
        if (ownTone)
            initializeOwnTone();
        // External app source is launched by disposable LAB runner only,
        // *after* capture + physical monitor signal EXTERNAL_SOURCE_READY.
    }

    internal static void RunSelfTests()
    {
        int internalInits = 0;
        Initialize(ownTone: true, externalAppTone: false,
            initializeOwnTone: () => internalInits++);
        if (internalInits != 1)
            throw new InvalidOperationException(
                "Internal 440 Hz mode failed to initialize exactly once.");

        // Reproduces the old null dereference: external-app mode has no
        // bridge-owned toneStream/WasapiOut at all. No hardware required.
        Initialize(ownTone: false, externalAppTone: true,
            initializeOwnTone: () => throw new InvalidOperationException(
                "External app mode incorrectly initialized null internal tone."));
        if (internalInits != 1)
            throw new InvalidOperationException(
                "External source unexpectedly initialized internal 440 Hz tone.");

        foreach (var (own, external) in new[]
                 { (false, false), (true, true) })
        {
            bool rejected = false;
            try
            {
                Initialize(own, external, () => internalInits++);
            }
            catch (ArgumentException) { rejected = true; }
            if (!rejected || internalInits != 1)
                throw new InvalidOperationException(
                    "Missing or competing lab tone modes were accepted.");
        }
        bool nullActionRejected = false;
        try { Initialize(false, true, null!); }
        catch (ArgumentNullException) { nullActionRejected = true; }
        if (!nullActionRejected)
            throw new InvalidOperationException(
                "Missing physical-route initializer must be rejected.");
        Console.WriteLine(
            "PASS: internal tone initializes once; independent app route " +
            "skips null tone initialization; missing/mixed modes rejected. " +
            "No physical audio endpoint used.");
    }
}
