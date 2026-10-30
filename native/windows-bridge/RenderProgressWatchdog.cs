using System.Diagnostics;

namespace LouderMeAudioBridge;

/// <summary>
/// Console-thread-only safety guard for a selected, experimental WASAPI
/// bridge route. A PlaybackState.Playing flag does not prove that queued
/// source PCM continues to reach NAudio's real render callbacks.
/// This guard observes queue occupancy and ACTUAL payload frames handed
/// to WASAPI; it never claims loudspeaker movement or acoustic SPL.
/// </summary>
internal sealed class RenderProgressWatchdog
{
    private const int StallWindowSeconds = 3;
    private long _lastSampleTicks = -1;
    private long _lastEnqueuedFrames;
    private long _lastConsumedFrames;
    private long _stallSinceTicks = -1;

    /// <summary>
    /// Return true only when at least 50 ms of real PCM has remained
    /// buffered without any payload consumption for three seconds.
    /// Idle/silent/no-source routes have no buffered PCM and never trip.
    /// Inputs are monotonic cumulative counters except bufferedFrames,
    /// which reflects current queue occupancy and may fall after a flush.
    /// </summary>
    public bool Observe(
        long nowTicks,
        long enqueuedFrames,
        long consumedFrames,
        long bufferedFrames,
        int outputSampleRate)
    {
        if (nowTicks < 0 || enqueuedFrames < 0 || consumedFrames < 0 ||
            bufferedFrames < 0 || outputSampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(nowTicks),
                "Render-watchdog inputs must have valid nonnegative counters and sample rate.");

        if (_lastSampleTicks >= 0 &&
            (nowTicks < _lastSampleTicks ||
             enqueuedFrames < _lastEnqueuedFrames ||
             consumedFrames < _lastConsumedFrames))
            throw new InvalidDataException(
                "Render-watchdog clock or PCM counters moved backward.");

        bool payloadAdvanced = consumedFrames > _lastConsumedFrames;
        bool substantialQueue = bufferedFrames >=
            Math.Max(1, outputSampleRate / 20); // 50 ms

        // A real renderer read or a drained queue clears any old suspicion.
        // An empty source must never be treated as an audio-output failure.
        if (payloadAdvanced || !substantialQueue)
            _stallSinceTicks = -1;
        else if (_stallSinceTicks < 0)
            _stallSinceTicks = nowTicks;

        _lastSampleTicks = nowTicks;
        _lastEnqueuedFrames = enqueuedFrames;
        _lastConsumedFrames = consumedFrames;
        return _stallSinceTicks >= 0 &&
               nowTicks - _stallSinceTicks >=
                   StallWindowSeconds * (long)Stopwatch.Frequency;
    }

    public static void RunSelfTests()
    {
        static long At(double seconds) =>
            (long)Math.Round(seconds * Stopwatch.Frequency);
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        var guard = new RenderProgressWatchdog();
        Require(!guard.Observe(At(0), 0, 0, 0, 48000), "No-source startup failed.");
        Require(!guard.Observe(At(20), 0, 0, 0, 48000),
            "Twenty seconds of valid input silence must not trip.");
        Require(!guard.Observe(At(21), 4800, 0, 4800, 48000),
            "Buffered PCM should begin a watchdog window, not abort immediately.");
        Require(!guard.Observe(At(23.75), 4800, 0, 4800, 48000),
            "Renderer stall window must allow short scheduling delays.");
        Require(guard.Observe(At(24.01), 4800, 0, 4800, 48000),
            "PlaybackState.Playing with queued PCM and zero render progress must abort.");
        Require(!guard.Observe(At(24.25), 9600, 4800, 4800, 48000),
            "A genuine payload read must clear the watchdog.");
        Require(!guard.Observe(At(26), 9600, 4800, 4800, 48000),
            "Recovered render should start a new independent stall window.");
        Require(!guard.Observe(At(26.1), 9600, 4800, 0, 48000),
            "A drained queue must cancel the stall window.");
        Require(!guard.Observe(At(100), 9600, 4800, 0, 48000),
            "Idle input after playback must remain safe.");

        var streaming = new RenderProgressWatchdog();
        for (int i = 0; i < 40; ++i)
            Require(!streaming.Observe(At(i * 0.25), (i + 1) * 4800L,
                i * 4800L, 4800, 48000),
                "Healthy continuously consumed audio was incorrectly interrupted.");

        var partial = new RenderProgressWatchdog();
        Require(!partial.Observe(At(0), 2399, 0, 2399, 48000),
            "Small startup buffer incorrectly triggered a failure.");
        Require(!partial.Observe(At(30), 2399, 0, 2399, 48000),
            "Sub-50-ms queues should not trigger a false failure.");

        try
        {
            guard.Observe(At(99), 9600, 4800, 0, 48000);
            throw new InvalidOperationException("Backward time was accepted.");
        }
        catch (InvalidDataException) { }
        try
        {
            guard.Observe(At(101), 9000, 4800, 0, 48000);
            throw new InvalidOperationException("Decreasing enqueue count was accepted.");
        }
        catch (InvalidDataException) { }
        try
        {
            guard.Observe(At(101), 9600, 4799, 0, 48000);
            throw new InvalidOperationException("Decreasing consumed count was accepted.");
        }
        catch (InvalidDataException) { }
        try
        {
            guard.Observe(At(101), 9600, 4800, 0, 0);
            throw new InvalidOperationException("Zero sample rate was accepted.");
        }
        catch (ArgumentOutOfRangeException) { }

        Console.WriteLine(
            "PASS: render-progress watchdog detects frozen physical payload reads, " +
            "accepts idle/small queues and healthy render, recovers after consumption, " +
            "and rejects invalid clock/counter data; device-free only.");
    }
}
