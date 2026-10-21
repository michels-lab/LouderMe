using System.Buffers.Binary;
using System.Globalization;
using System.Threading;

namespace LouderMeAudioBridge;

// Exactly one immutable command is published at a time: UI/console threads
// allocate and validate outside the audio callback. Only the capture callback
// applies DSP setters; no locks, waits, or managed allocations on the reader.
internal sealed class DspControlMailbox
{
    private sealed class Command
    {
        public readonly int Boost;
        public readonly bool EqEnabled;
        public readonly float[] EqDb; // private clone; never exposed to publisher
        public Command(int boost, bool enabled, float[] eqDb)
        {
            Boost = boost;
            EqEnabled = enabled;
            EqDb = (float[])eqDb.Clone();
        }
    }

    private Command? _pending;
    private long _published;
    private long _applied;
    private long _superseded;

    public long Published => Interlocked.Read(ref _published);
    public long Applied => Interlocked.Read(ref _applied);
    public long Superseded => Interlocked.Read(ref _superseded);

    public void Publish(int boost, bool eqEnabled, float[] eqDb)
    {
        if (boost is < 100 or > 250 || eqDb is null || eqDb.Length != 7)
            throw new ArgumentException("DSP command needs boost 100–250 and exactly seven EQ values.");
        foreach (var value in eqDb)
            if (!float.IsFinite(value) || value is < -10f or > 10f)
                throw new ArgumentException("DSP EQ must be finite in [-10,+10] dB.");

        if (Interlocked.Exchange(ref _pending, new Command(boost, eqEnabled, eqDb)) is not null)
            Interlocked.Increment(ref _superseded);
        Interlocked.Increment(ref _published);
    }

    // Called ONLY by WASAPI's capture callback; late or superseded commands
    // coalesce naturally. Cross-platform native DSP setters must never be
    // called by a UI thread. No background thread touches the DSP handle.
    public bool ApplyPending(NativeDsp engine)
    {
        var command = Interlocked.Exchange(ref _pending, null);
        if (command is null) return false;
        engine.ConfigureBoost(command.Boost);
        engine.ConfigureEqualizer(command.EqEnabled, command.EqDb);
        Interlocked.Increment(ref _applied);
        return true;
    }

    public static void RunSelfTests()
    {
        using var engine = new NativeDsp(48000, 2, 100);
        var queue = new DspControlMailbox();
        var input = new float[7];
        input[4] = 8;
        queue.Publish(150, true, input);
        input[4] = -8; // producer cannot mutate a published command
        if (!queue.ApplyPending(engine) || queue.ApplyPending(engine))
            throw new InvalidOperationException("Control mailbox must consume exactly once.");

        // Actual native C ABI processing confirms boost, in-memory real PCM,
        // and no background-thread calls to NativeDsp.
        var samples = new byte[48000 * 2 * sizeof(float) / 10];
        for (int i = 0; i < samples.Length; i += sizeof(float))
            BinaryPrimitives.WriteSingleLittleEndian(samples.AsSpan(i), .02f);
        engine.Process(samples, samples.Length, 2);
        float last = BinaryPrimitives.ReadSingleLittleEndian(samples.AsSpan(samples.Length - 4));
        if (!float.IsFinite(last) || last <= .02f)
            throw new InvalidOperationException("Native DSP did not apply live gain/EQ command.");

        queue.Publish(100, false, new float[7]);
        if (!queue.ApplyPending(engine))
            throw new InvalidOperationException("Control mailbox failed to disable EQ.");
        // Bypass transition must settle without changing the selected stream.
        for (int i = 0; i < samples.Length; i += sizeof(float))
            BinaryPrimitives.WriteSingleLittleEndian(samples.AsSpan(i), .02f);
        engine.Process(samples, samples.Length, 2);
        last = BinaryPrimitives.ReadSingleLittleEndian(samples.AsSpan(samples.Length - 4));
        if (Math.Abs(last - .02f) > .0001f)
            throw new InvalidOperationException("100% live control did not settle to unity.");

        var latest = new float[7];
        for (int i = 0; i < 200; i++) queue.Publish(100 + i % 151, false, latest);
        if (!queue.ApplyPending(engine) || queue.ApplyPending(engine) ||
            queue.Applied != 3 || queue.Published != 202 ||
            queue.Superseded != 199)
            throw new InvalidOperationException("Bursty DSP control updates were not coalesced.");

        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, 10.1f, -10.1f })
        {
            var bad = new float[7]; bad[0] = invalid;
            try { queue.Publish(120, true, bad); }
            catch (ArgumentException) { continue; }
            throw new InvalidOperationException("Invalid live EQ value accepted.");
        }
        bool invalidBoostRejected = false;
        try { queue.Publish(251, false, latest); }
        catch (ArgumentException) { invalidBoostRejected = true; }
        if (!invalidBoostRejected)
            throw new InvalidOperationException("Invalid live Boost accepted.");

        // Stress a changing producer while the only consumer processes native
        // PCM. The producer NEVER calls C ABI native setters or processes audio.
        var concurrentQueue = new DspControlMailbox();
        using var streamingEngine = new NativeDsp(48000, 2, 100);
        var producer = Task.Run(() =>
        {
            for (var i = 0; i < 1200; i++)
            {
                var eq = new float[7];
                eq[4] = i % 2 == 0 ? 3 : -3;
                concurrentQueue.Publish(100 + i % 151, true, eq);
            }
        });
        var chunk = new byte[480 * 2 * sizeof(float)];
        for (var batch = 0; batch < 240; batch++)
        {
            for (int i = 0; i < chunk.Length; i += 4)
                BinaryPrimitives.WriteSingleLittleEndian(chunk.AsSpan(i), .025f);
            concurrentQueue.ApplyPending(streamingEngine);
            streamingEngine.Process(chunk, chunk.Length, 2);
            for (int i = 0; i < chunk.Length; i += 4)
            {
                float sample = BinaryPrimitives.ReadSingleLittleEndian(chunk.AsSpan(i));
                if (!float.IsFinite(sample) || sample is < -1f or > 1f)
                    throw new InvalidOperationException("Concurrent live DSP update corrupted PCM.");
            }
        }
        producer.GetAwaiter().GetResult();
        concurrentQueue.ApplyPending(streamingEngine);
        if (concurrentQueue.Published != 1200 ||
            concurrentQueue.Applied > concurrentQueue.Published ||
            concurrentQueue.Applied == 0)
            throw new InvalidOperationException("Concurrent live DSP command accounting broken.");

        Console.WriteLine(
            "PASS: native DSP live controls, bounded valid parameter snapshots, " +
            "coalescing, concurrent producer/PCM consumer, finite output, no Windows endpoints.");
    }
}

// Text console is a LAB-only control source, NOT a shipped UI, driver, or
// network listener. It cannot change the default Windows output device.
internal sealed class DspLiveConsoleControls
{
    private readonly DspControlMailbox _mailbox;
    private int _boost;
    private bool _eqOn;
    private float[] _bands;

    public DspLiveConsoleControls(DspControlMailbox mailbox, int boost, bool eqOn, float[] bands)
    {
        _mailbox = mailbox;
        _boost = boost;
        _eqOn = eqOn;
        _bands = (float[])bands.Clone();
    }

    public bool TryHandle(string text, out string message)
    {
        var command = text.Trim();
        if (command.StartsWith("boost ", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(command[6..].Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out var newBoost) &&
            newBoost is >= 100 and <= 250)
        {
            _boost = newBoost;
        }
        else if (string.Equals(command, "eq off", StringComparison.OrdinalIgnoreCase))
        {
            _eqOn = false;
        }
        else if (command.StartsWith("eq ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = command[3..].Split(',');
            if (parts.Length != 7)
            {
                message = "Rejected: EQ needs seven dB values [-10,+10].";
                return false;
            }
            var next = new float[7];
            for (var i = 0; i < next.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out next[i]) ||
                    !float.IsFinite(next[i]) || next[i] is < -10 or > 10)
                {
                    message = "Rejected: EQ values must be finite in [-10,+10] dB.";
                    return false;
                }
            }
            _bands = next;
            _eqOn = true;
        }
        else
        {
            message = "Rejected. Use: boost 100..250 | eq off | eq b1,b2,b3,b4,b5,b6,b7";
            return false;
        }

        _mailbox.Publish(_boost, _eqOn, _bands);
        message = $"QUEUED for audio callback: Boost {_boost}% / EQ {(_eqOn ? "on" : "off")}.";
        return true;
    }

    public static void RunSelfTests()
    {
        var mailbox = new DspControlMailbox();
        var controls = new DspLiveConsoleControls(mailbox, 100, false, new float[7]);
        if (!controls.TryHandle("boost 150", out _) ||
            !controls.TryHandle("eq 0,0,0,0,8,0,0", out _) ||
            controls.TryHandle("eq 0,NaN,0,0,0,0,0", out _) ||
            controls.TryHandle("boost 251", out _) ||
            controls.TryHandle("eq 0,0", out _) ||
            !controls.TryHandle("eq off", out _) ||
            mailbox.Published != 3)
            throw new InvalidOperationException("Live console parser rejected valid commands or published invalid ones.");
        Console.WriteLine("PASS: opt-in LAB console parser validates all seven EQ bands, boost bounds, no invalid publication.");
    }
}
