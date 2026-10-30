using System.Buffers.Binary;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Opt-in, low-level native DSP -> physical WASAPI loopback experiment.
/// Unlike the virtual-source route, this proves a SEPARATE capability:
/// the owned C++ DSP and a user-selected physical playback endpoint can
/// exchange actual PCM, even before an installable virtual driver exists.
/// It NEVER intercepts Windows applications and NEVER changes defaults.
/// </summary>
internal static class DirectPhysicalDspProbe
{
    private const float InputAmplitude = 0.025f;
    private const int ToneSeconds = 2;
    private const int QuietPaddingMs = 250;

    internal static float[]? ParseEq(string? values)
    {
        if (values is null) return null;
        var parts = values.Split(',');
        if (parts.Length != 7)
            throw new ArgumentException(
                "Direct physical DSP probe requires exactly seven EQ bands.");
        var gains = new float[7];
        for (int i = 0; i < parts.Length; i++)
            if (!float.TryParse(parts[i],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out gains[i]) ||
                !float.IsFinite(gains[i]) || gains[i] is < -10 or > 10)
                throw new ArgumentException(
                    "Direct physical DSP EQ bands must be finite [-10,+10] dB.");
        return gains;
    }

    internal static byte[] PreparePcm(
        WaveFormat outputMix, int boost, float[]? eqGains = null)
    {
        ArgumentNullException.ThrowIfNull(outputMix);
        if (!PcmFormatAdapter.IsSupported(outputMix) || outputMix.Channels is < 1 or > 2)
            throw new NotSupportedException(
                "Direct LAB output needs PCM16/float32 mono or stereo, 8–192 kHz.");
        if (boost is < 100 or > 150)
            throw new ArgumentOutOfRangeException(nameof(boost),
                "Physical experiment is limited to 100–150% gain.");
        if (eqGains is not null &&
            (eqGains.Length != 7 ||
             eqGains.Any(value => !float.IsFinite(value) || value is < -10 or > 10)))
            throw new ArgumentException("LAB EQ must contain seven finite gains in ±10 dB.",
                nameof(eqGains));

        int rate = outputMix.SampleRate;
        int channels = outputMix.Channels;
        int paddingFrames = checked(rate * QuietPaddingMs / 1000);
        int toneFrames = checked(rate * ToneSeconds);
        int totalFrames = checked(toneFrames + 2 * paddingFrames);
        var floatMix = WaveFormat.CreateIeeeFloatWaveFormat(rate, channels);
        var floatPcm = new byte[checked(totalFrames * floatMix.BlockAlign)];
        for (int frame = 0; frame < toneFrames; frame++)
        {
            float value = InputAmplitude * MathF.Sin(
                2f * MathF.PI * LoopbackToneVerifier.FrequencyHz * frame / rate);
            for (int channel = 0; channel < channels; channel++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    floatPcm.AsSpan(((paddingFrames + frame) * channels + channel) *
                                    sizeof(float)), value);
        }

        // This is the same owned native C++ engine and PCM format adapter
        // as the virtual->physical bridge, not an Equalizer APO test.
        using (var native = new NativeDsp(rate, channels, boost))
        {
            if (eqGains is not null)
                native.ConfigureEqualizer(true, eqGains);
            native.Process(floatPcm, floatPcm.Length, channels);
        }
        return new PcmFormatAdapter(floatMix, outputMix)
            .Encode(floatPcm, floatPcm.Length);
    }

    internal static int Run(MMDevice output, int boost, float[]? eqGains)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (output.State != DeviceState.Active ||
            output.FriendlyName.Contains("LouderMe Virtual",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Direct experiment requires an active PHYSICAL output, not a virtual endpoint.");

        var mix = output.AudioClient.MixFormat;
        var payload = PreparePcm(mix, boost, eqGains);
        using var source = new RawSourceWaveStream(
            new MemoryStream(payload, writable: false), mix);
        using var playback = new WasapiOut(
            output, AudioClientShareMode.Shared, true, 75);
        using var monitor = new WasapiLoopbackCapture(output);
        var captureFormat = monitor.WaveFormat;
        if (!PcmFormatAdapter.IsSupported(captureFormat) ||
            captureFormat.Channels is < 1 or > 2)
            throw new NotSupportedException(
                "Selected physical loopback needs PCM16/float32 mono/stereo.");
        var verifierFormat = WaveFormat.CreateIeeeFloatWaveFormat(
            captureFormat.SampleRate, captureFormat.Channels);
        var decoder = new PcmFormatAdapter(captureFormat, verifierFormat);
        var verifier = new LoopbackToneVerifier(verifierFormat);
        var gate = new object();
        Exception? callbackFailure = null;
        monitor.RecordingStopped += (_, e) =>
        {
            if (e.Exception is not null)
                Interlocked.CompareExchange(ref callbackFailure, e.Exception, null);
        };
        monitor.DataAvailable += (_, e) =>
        {
            lock (gate)
            {
                try
                {
                    if (e.BytesRecorded > 0)
                        verifier.Add(decoder.Decode(e.Buffer, e.BytesRecorded));
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref callbackFailure, ex, null);
                }
            }
        };
        playback.PlaybackStopped += (_, e) =>
        {
            if (e.Exception is not null)
                Interlocked.CompareExchange(ref callbackFailure, e.Exception, null);
        };

        playback.Init(source);
        using var cancelEvent = new ManualResetEventSlim(false);
        ConsoleCancelEventHandler cancel = (_, args) =>
        {
            args.Cancel = true;
            cancelEvent.Set();
        };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.WriteLine(
                "DIRECT_NATIVE_DSP_LAB: selected physical output only; " +
                "no virtual driver, no default change, no third-party audio.");
            Console.WriteLine(
                "Low-level quiet 440 Hz stimulus, 2 seconds, native gain " +
                boost + "%; 250 ms silence before and after.");
            monitor.StartRecording();
            playback.Play();
            var started = Stopwatch.GetTimestamp();
            // The waveform ends at 2.5s; an additional short interval lets
            // WASAPI flush the monitor callbacks. Bounded and Ctrl+C-safe.
            while (!cancelEvent.Wait(100) &&
                   Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(4))
            {
                if (Volatile.Read(ref callbackFailure) is not null)
                    throw new IOException(
                        "Physical WASAPI playback/monitor callback failed.",
                        callbackFailure);
                if (output.State != DeviceState.Active)
                    throw new IOException("Selected physical output disconnected.");
            }
            if (cancelEvent.IsSet)
                throw new OperationCanceledException(
                    "The lab playback was cancelled; no evidence accepted.");
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
            playback.Stop();
            monitor.StopRecording();
        }

        if (callbackFailure is not null)
            throw new IOException("Physical WASAPI monitor or playback failed.",
                callbackFailure);
        LoopbackToneVerifier.ToneEvidence evidence;
        lock (gate)
            evidence = verifier.Evaluate();

        Console.WriteLine(
            $"DIRECT_PHYSICAL_PCM: monitorFrames={evidence.Frames} " +
            $"rms={evidence.Rms:F5} peak={evidence.Peak:F5} " +
            $"matched440={evidence.Matched440Amplitude:F5} " +
            $"coherence={evidence.Coherence:F3}");
        if (!evidence.Passed)
        {
            Console.Error.WriteLine(
                "FAIL: selected physical WASAPI loopback did not reproduce " +
                "the native DSP test tone. No physical-route proof.");
            return 4;
        }

        Console.WriteLine(
            "PASS: owned DSP -> selected physical WASAPI loopback " +
            "440 Hz signal identity. NOT proof of acoustic audibility, " +
            "driver signing, gain ratio, or system-wide third-party routing.");
        return 0;
    }

    internal static void RunSelfTests()
    {
        var formats = new WaveFormat[]
        {
            new WaveFormat(44100, 16, 1),
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 2),
            WaveFormat.CreateIeeeFloatWaveFormat(96000, 1)
        };
        foreach (var format in formats)
        {
            double[] amplitude = new double[2];
            for (int index = 0; index < 2; index++)
            {
                int boost = index == 0 ? 100 : 150;
                byte[] pcm = PreparePcm(format, boost);
                int expectedFrames = format.SampleRate * (ToneSeconds * 1000 +
                    2 * QuietPaddingMs) / 1000;
                if (pcm.Length != expectedFrames * format.BlockAlign)
                    throw new InvalidOperationException(
                        "Direct PCM packet size does not match the actual output mix.");
                var decoder = new PcmFormatAdapter(
                    format, WaveFormat.CreateIeeeFloatWaveFormat(
                        format.SampleRate, format.Channels));
                var decoded = decoder.Decode(pcm, pcm.Length);
                var verifier = new LoopbackToneVerifier(
                    WaveFormat.CreateIeeeFloatWaveFormat(
                        format.SampleRate, format.Channels));
                verifier.Add(decoded);
                var measured = verifier.Evaluate();
                if (!measured.Passed || measured.Peak >= 0.15)
                    throw new InvalidOperationException(
                        "Direct C++ DSP PCM stimulus failed bounded 440 Hz identity.");
                amplitude[index] = measured.Matched440Amplitude;
            }
            double ratio = amplitude[1] / amplitude[0];
            if (!double.IsFinite(ratio) || ratio is < 1.25 or > 1.75)
                throw new InvalidOperationException(
                    "Native DSP physical-preview stimulus did not show 100->150% " +
                    "synthetic amplitude gain.");
        }

        if (ParseEq(null) is not null || ParseEq("0,0,0,0,8,0,0") is not { Length: 7 })
            throw new InvalidOperationException(
                "Direct EQ parser rejected a valid optional seven-band command.");
        foreach (var invalid in new[] { "0,0", "0,NaN,0,0,0,0,0", "11,0,0,0,0,0,0" })
        {
            bool rejected = false;
            try { ParseEq(invalid); }
            catch (ArgumentException) { rejected = true; }
            if (!rejected)
                throw new InvalidOperationException(
                    "Invalid direct-output EQ command was accepted.");
        }
        var fmt = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        foreach (int boost in new[] { 99, 151, 250 })
        {
            bool rejected = false;
            try { PreparePcm(fmt, boost); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            if (!rejected)
                throw new InvalidOperationException(
                    "Direct physical probe accepted unsafe gain.");
        }
        try
        {
            PreparePcm(WaveFormat.CreateIeeeFloatWaveFormat(48000, 6), 100);
            throw new InvalidOperationException(
                "Unverified multichannel physical output was accepted.");
        }
        catch (NotSupportedException) { }
        try
        {
            PreparePcm(fmt, 100, new float[] { float.NaN, 0, 0, 0, 0, 0, 0 });
            throw new InvalidOperationException("NaN EQ was accepted.");
        }
        catch (ArgumentException) { }

        Console.WriteLine(
            "PASS: owned C++ DSP -> physical-format PCM16/float32 440 Hz " +
            "2s preview at 44.1/48/96 kHz mono/stereo, synthetic 100/150% gain, " +
            "low-volume bound, invalid EQ/boost/channel rejection. No hardware.");
    }
}
