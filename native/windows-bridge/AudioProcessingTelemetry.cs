using System.Diagnostics;
using System.Threading;

namespace LouderMeAudioBridge;

/// <summary>
/// Thread-safe native+managed per-callback work timing. CPU busy ratio is
/// calculated from the nominal duration of captured PCM, not from actual
/// OS process CPU scheduling time. Queue duration is NOT acoustic latency.
/// </summary>
internal sealed class AudioProcessingTelemetry
{
    private long _chunks;
    private long _totalWorkMicroseconds;
    private long _totalAudioMicroseconds;
    private long _worstWorkMicroseconds;
    private long _overBudgetChunks;

    public void Record(long elapsedStopwatchTicks, int inputFrames, int sampleRate)
    {
        if (elapsedStopwatchTicks < 0 || inputFrames <= 0 || sampleRate <= 0)
            return;

        long microseconds = (long)Math.Round(
            (double)elapsedStopwatchTicks * 1_000_000 / Stopwatch.Frequency);
        long audioMicroseconds = (long)Math.Round(
            (double)inputFrames * 1_000_000 / sampleRate);

        Interlocked.Increment(ref _chunks);
        Interlocked.Add(ref _totalWorkMicroseconds, microseconds);
        Interlocked.Add(ref _totalAudioMicroseconds, audioMicroseconds);
        if (microseconds > audioMicroseconds)
            Interlocked.Increment(ref _overBudgetChunks);

        long prior = Interlocked.Read(ref _worstWorkMicroseconds);
        while (microseconds > prior)
        {
            long current = Interlocked.CompareExchange(
                ref _worstWorkMicroseconds, microseconds, prior);
            if (current == prior) break;
            prior = current;
        }
    }

    public Snapshot Read() => new(
        Interlocked.Read(ref _chunks),
        Interlocked.Read(ref _totalWorkMicroseconds),
        Interlocked.Read(ref _totalAudioMicroseconds),
        Interlocked.Read(ref _worstWorkMicroseconds),
        Interlocked.Read(ref _overBudgetChunks));

    internal readonly record struct Snapshot(
        long Chunks, long WorkMicroseconds, long AudioMicroseconds,
        long WorstWorkMicroseconds, long OverBudgetChunks)
    {
        public double WorkToAudioPercent => AudioMicroseconds > 0
            ? 100.0 * WorkMicroseconds / AudioMicroseconds : 0;
        public double WorstWorkMs => WorstWorkMicroseconds / 1000.0;
    }

    public static void RunSelfTests()
    {
        var meter = new AudioProcessingTelemetry();
        long oneMillisecondTicks = Stopwatch.Frequency / 1000;
        meter.Record(oneMillisecondTicks, 480, 48000); // 10 ms of PCM
        meter.Record(oneMillisecondTicks * 12, 480, 48000); // over-budget
        meter.Record(-1, 480, 48000); // ignored
        var snap = meter.Read();
        if (snap.Chunks != 2 || snap.OverBudgetChunks != 1 ||
            snap.WorkToAudioPercent is < 62 or > 68 ||
            snap.WorstWorkMs is < 11.5 or > 12.5)
            throw new InvalidOperationException("Audio processing timing metrics regression.");
        Console.WriteLine(
            "PASS: work-to-audio processing time, over-budget packets, worst callback.");
    }
}
