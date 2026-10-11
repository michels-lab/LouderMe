using System.Diagnostics;
using System.Threading;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Bounded queue recovery for an isolated experimental render path.
/// A run of overflows discards stale PCM only twice within 30 seconds;
/// continued overflow fails closed instead of replaying increasingly stale
/// sound, silently overriding a physical device or spinning forever.
/// </summary>
internal sealed class AudioQueueRecovery
{
    internal enum Action { DropNewest, FlushStaleAndAccept, AbortRoute }

    private long _windowStartTicks = -1;
    private int _consecutiveOverflows;
    private int _resetsInWindow;

    public Action OnOverflow(long nowTicks)
    {
        if (nowTicks < 0) throw new ArgumentOutOfRangeException(nameof(nowTicks));
        if (_windowStartTicks < 0 || nowTicks - _windowStartTicks >=
            30L * Stopwatch.Frequency)
        {
            _windowStartTicks = nowTicks;
            _consecutiveOverflows = 0;
            _resetsInWindow = 0;
        }

        if (++_consecutiveOverflows < 3)
            return Action.DropNewest;

        _consecutiveOverflows = 0;
        if (++_resetsInWindow <= 2)
            return Action.FlushStaleAndAccept;
        return Action.AbortRoute;
    }

    public void OnHealthyWrite() => _consecutiveOverflows = 0;

    public static void RunSelfTests()
    {
        var policy = new AudioQueueRecovery();
        long now = Stopwatch.Frequency;
        for (int i = 0; i < 2; i++)
            if (policy.OnOverflow(now) != Action.DropNewest)
                throw new InvalidOperationException("Overflow burst should first drop incoming PCM.");
        if (policy.OnOverflow(now) != Action.FlushStaleAndAccept)
            throw new InvalidOperationException("Three contiguous overflow events must flush stale PCM.");
        policy.OnHealthyWrite();
        if (policy.OnOverflow(now) != Action.DropNewest)
            throw new InvalidOperationException("Healthy writes must reset burst counter.");
        policy.OnOverflow(now);
        if (policy.OnOverflow(now) != Action.FlushStaleAndAccept)
            throw new InvalidOperationException("Second bounded flush was unavailable.");
        policy.OnHealthyWrite();
        if (policy.OnOverflow(now) != Action.DropNewest ||
            policy.OnOverflow(now) != Action.DropNewest ||
            policy.OnOverflow(now) != Action.AbortRoute)
            throw new InvalidOperationException("Persistent overflow must fail closed after two flushes.");
        if (policy.OnOverflow(now + 31L * Stopwatch.Frequency) != Action.DropNewest)
            throw new InvalidOperationException("Recovery window was not reset after 30 seconds.");
        try
        {
            policy.OnOverflow(-1);
            throw new InvalidOperationException("Negative timestamp accepted.");
        }
        catch (ArgumentOutOfRangeException) { }

        var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var queued = new BufferedWaveProvider(
            format, TimeSpan.FromMilliseconds(250))
        {
            ReadFully = true
        };
        var monitor = new MonitoredRenderBuffer(queued);
        byte[] read = new byte[format.BlockAlign * 10];
        if (monitor.Read(read, 0, read.Length) != read.Length ||
            monitor.ReadsWithInsufficientPcm != 1)
            throw new InvalidOperationException("Playback underflow was not observed.");
        byte[] input = new byte[read.Length];
        queued.AddSamples(input, 0, input.Length);
        monitor.Read(read, 0, read.Length);
        if (monitor.ReadsWithInsufficientPcm != 1)
            throw new InvalidOperationException("Fully supplied output must not count as underflow.");
        Console.WriteLine("PASS: bounded queue flush, escalation, recovery window and playback starvation observations.");
    }
}

/// <summary>
/// Monitors PCM available before each physical render callback. NAudio's
/// ReadFully may zero-fill the requested bytes: this is a conservative
/// queue-starvation indicator, not a measured WASAPI hardware underrun.
/// </summary>
internal sealed class MonitoredRenderBuffer : IWaveProvider
{
    private readonly BufferedWaveProvider _source;
    private long _readsWithInsufficientPcm;

    public MonitoredRenderBuffer(BufferedWaveProvider source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public WaveFormat WaveFormat => _source.WaveFormat;
    public long ReadsWithInsufficientPcm => Interlocked.Read(ref _readsWithInsufficientPcm);

    public int Read(byte[] data, int offset, int count)
    {
        int available = _source.BufferedBytes;
        int read = _source.Read(data.AsSpan(offset, count));
        if (available < count)
            Interlocked.Increment(ref _readsWithInsufficientPcm);
        return read;
    }

    // NAudio 3 IWaveProvider additionally requires Span<byte> reads.
    public int Read(Span<byte> data)
    {
        int available = _source.BufferedBytes;
        int read = _source.Read(data);
        if (available < data.Length)
            Interlocked.Increment(ref _readsWithInsufficientPcm);
        return read;
    }
}

internal sealed class AudioRouteRecoverableException : IOException
{
    public AudioRouteRecoverableException(string message) : base(message) { }
}
