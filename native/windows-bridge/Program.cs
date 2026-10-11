using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Buffers.Binary;
using System.Diagnostics;

namespace LouderMeAudioBridge;

internal static class Program
{
    // IEEE 754 floating PCM subtype in WAVEFORMATEXTENSIBLE.
    private static readonly Guid Float32Subtype =
        new("00000003-0000-0010-8000-00aa00389b71");

    private static bool IsFloat32(WaveFormat format) =>
        format.BitsPerSample == 32 &&
        (format.Encoding == WaveFormatEncoding.IeeeFloat ||
         (format is WaveFormatExtensible extended && extended.SubFormat == Float32Subtype));

    private static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--check-native"))
                return CheckNative();
            if (args.Contains("--check-pipeline"))
                return CheckPipeline();
            if (args.Contains("--check-loopback"))
            {
                LoopbackToneVerifier.RunSelfTests();
                return 0;
            }
            if (args.Contains("--check-formats"))
            {
                PcmFormatAdapter.RunSelfTests();
                return 0;
            }

            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            if (args.Contains("--list-devices"))
            {
                foreach (var device in devices)
                    Console.WriteLine($"[{device.FriendlyName}] {device.ID}");
                return 0;
            }

            if (args.Contains("--probe-virtual"))
                return ProbeVirtualEndpoint(devices, args);

            // Never install a driver or change Windows' default endpoint.
            var sourceId = GetArg(args, "--source-id");
            var outputId = GetArg(args, "--output-id");
            var percentText = GetArg(args, "--boost");
            if (!args.Contains("--confirm-experimental") ||
                string.IsNullOrWhiteSpace(sourceId) ||
                string.IsNullOrWhiteSpace(outputId) ||
                !int.TryParse(percentText, out var boost) || boost is < 100 or > 250)
            {
                Console.Error.WriteLine(
                    "Experimental only. Use --list-devices, then " +
                    "--source-id <LOUDERME_VIRTUAL_ENDPOINT_ID> --output-id <PHYSICAL_ENDPOINT_ID> " +
                    "--boost <100-250> [--eq-db b1,b2,b3,b4,b5,b6,b7] --confirm-experimental");
                return 2;
            }

            if (sourceId == outputId)
                throw new InvalidOperationException("Capture and physical output endpoints must differ.");

            var source = devices.FirstOrDefault(x => x.ID == sourceId)
                ?? throw new InvalidOperationException("Selected virtual capture endpoint not active.");
            var physical = devices.FirstOrDefault(x => x.ID == outputId)
                ?? throw new InvalidOperationException("Selected physical output endpoint not active.");

            // Fail closed: capturing arbitrary physical output would cause doubled sound/feedback.
            if (!source.FriendlyName.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase) ||
                 physical.FriendlyName.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The source must be an explicitly selected LouderMe Virtual output, " +
                    "and the target must be a separate physical playback device.");

            using var capture = new WasapiLoopbackCapture(source);
            var inputFormat = capture.WaveFormat;
            var outputFormat = physical.AudioClient.MixFormat;
            // Decode to native float32 and adapt the processed stream to the
            // actual physical mix format; channel layouts are still required
            // to match until a measured channel-mapping policy exists.
            var adapter = new PcmFormatAdapter(inputFormat, outputFormat);

            using var engine = new NativeDsp(
                inputFormat.SampleRate, inputFormat.Channels, boost);
            float[] eqGains = new float[7];
            var eqText = GetArg(args, "--eq-db");
            if (eqText is not null)
            {
                var parts = eqText.Split(',');
                if (parts.Length != 7)
                    throw new ArgumentException("Expected seven comma-separated EQ dB values.");
                for (var i = 0; i < 7; i++)
                {
                    if (!float.TryParse(parts[i],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out eqGains[i]) || !float.IsFinite(eqGains[i]) ||
                        eqGains[i] is < -10 or > 10)
                        throw new ArgumentException("EQ gains must be finite values from -10 to +10 dB.");
                }
            }
            engine.ConfigureEqualizer(eqText is not null, eqGains);
            using var playback = new WasapiOut(physical, AudioClientShareMode.Shared, true, 75);
            var buffer = new BufferedWaveProvider(outputFormat, TimeSpan.FromMilliseconds(250))
            {
                DiscardOnBufferOverflow = false,
                ReadFully = true
            };
            playback.Init(buffer);

            long processedFrames = 0;
            long outputFrames = 0;
            long droppedFrames = 0;
            var recordingStopped = new ManualResetEventSlim(false);
            Exception? recordingError = null;
            Exception? callbackError = null;
            capture.RecordingStopped += (_, e) =>
            {
                recordingError = e.Exception;
                recordingStopped.Set();
            };
            capture.DataAvailable += (_, e) =>
            {
                if (recordingStopped.IsSet) return;
                try
                {
                    if (e.BytesRecorded == 0) return;
                    // Never mutate NAudio's recycled event buffer. Native DSP
                    // receives an independent float32 copy even for PCM16 input.
                    byte[] decoded = adapter.Decode(e.Buffer, e.BytesRecorded);
                    engine.Process(decoded, decoded.Length, inputFormat.Channels);
                    byte[] converted = adapter.Encode(decoded, decoded.Length);
                    if (converted.Length == 0) return;
                    var sourceFrames = e.BytesRecorded / inputFormat.BlockAlign;
                    if (buffer.BufferLength - buffer.BufferedBytes < converted.Length)
                    {
                        Interlocked.Add(ref droppedFrames, sourceFrames);
                        return;
                    }
                    buffer.AddSamples(converted, 0, converted.Length);
                    Interlocked.Add(ref processedFrames, sourceFrames);
                    Interlocked.Add(ref outputFrames, converted.Length / outputFormat.BlockAlign);
                }
                catch (Exception error)
                {
                    // Managed exceptions must not escape a WASAPI callback;
                    // fail closed on the console thread and release endpoints.
                    Interlocked.CompareExchange(ref callbackError, error, null);
                    recordingStopped.Set();
                }
            };

            using var shutdown = new ManualResetEventSlim(false);
            ConsoleCancelEventHandler cancel = (_, e) =>
            {
                e.Cancel = true;
                shutdown.Set();
            };
            Console.CancelKeyPress += cancel;
            try
            {
                Console.WriteLine("Experimental routing. System default device is NOT changed.");
                Console.WriteLine($"Source: {source.FriendlyName}");
                Console.WriteLine($"Output: {physical.FriendlyName}");
                Console.WriteLine($"DSP: {boost}% gain, own C++ engine, EQ " +
                    (eqText is null ? "disabled" : $"enabled ({eqText} dB)"));
                Console.WriteLine($"Formats: virtual {inputFormat}; physical {outputFormat}");
                Console.WriteLine("PCM16/float32 conversion + linear rate adaptation; matching channel count required.");
                Console.WriteLine("Press Ctrl+C to stop; this is not a shipped system driver.");

                playback.Play();
                capture.StartRecording();
                while (!shutdown.Wait(1000))
                {
                    if (recordingStopped.IsSet)
                    {
                        if (callbackError is not null)
                            throw new IOException("PCM bridge callback failed.", callbackError);
                        if (recordingError is not null) throw recordingError;
                        throw new IOException("Virtual capture stopped unexpectedly.");
                    }
                    if (playback.PlaybackState != PlaybackState.Playing)
                        throw new IOException("Physical audio playback stopped.");
                    Console.WriteLine($"sourceFrames={Interlocked.Read(ref processedFrames)} " +
                        $"physicalFrames={Interlocked.Read(ref outputFrames)} " +
                        $"droppedSourceFrames={Interlocked.Read(ref droppedFrames)} " +
                        $"buffered={buffer.BufferedDuration.TotalMilliseconds:F0}ms");
                }
            }
            finally
            {
                Console.CancelKeyPress -= cancel;
                capture.StopRecording();
                recordingStopped.Wait(TimeSpan.FromSeconds(5));
                playback.Stop();
            }
            Console.WriteLine("Audio bridge stopped and endpoints released.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static int ProbeVirtualEndpoint(
        IEnumerable<MMDevice> devices, string[] args)
    {
        // Sound proof only: locally generate a low-amplitude test tone on a
        // specified virtual endpoint and measure its loopback. Does not alter
        // the default playback device or play to a physical output.
        if (!args.Contains("--confirm-experimental"))
            throw new ArgumentException("Probe requires explicit --confirm-experimental.");
        var sourceId = GetArg(args, "--source-id")
            ?? throw new ArgumentException("Probe requires --source-id.");
        var endpoint = devices.FirstOrDefault(d => d.ID == sourceId)
            ?? throw new InvalidOperationException("Selected endpoint not available.");
        if (!endpoint.FriendlyName.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Probe refuses non-LouderME source endpoints.");

        using var capture = new WasapiLoopbackCapture(endpoint);
        var format = capture.WaveFormat;
        if (!IsFloat32(format) || format.Channels is < 1 or > 8 ||
            format.SampleRate is < 8000 or > 192000)
            throw new NotSupportedException("Probe needs a valid float32 virtual mix format.");
        var frames = format.SampleRate * 2;
        var audio = new byte[checked(frames * format.BlockAlign)];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = 0.025f * MathF.Sin(
                2 * MathF.PI * 440f * frame / format.SampleRate);
            for (var ch = 0; ch < format.Channels; ch++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    audio.AsSpan((frame * format.Channels + ch) * sizeof(float)), value);
        }

        // A nonzero stream alone can be unrelated system audio or DC noise.
        // Prove that captured PCM has the 440 Hz identity of OUR known tone,
        // with continuous phase across callback packets and unknown start lag.
        var verifier = new LoopbackToneVerifier(format);
        var sync = new object();
        Exception? callbackError = null;
        Exception? captureError = null;
        capture.RecordingStopped += (_, e) =>
        {
            if (e.Exception is not null) captureError = e.Exception;
        };
        capture.DataAvailable += (_, e) =>
        {
            lock (sync)
            {
                try
                {
                    verifier.Add(e.Buffer.AsSpan(0, e.BytesRecorded));
                }
                catch (Exception ex)
                {
                    // NAudio's event thread must not throw and bring down
                    // the lab host. Report and fail closed on our main thread.
                    callbackError ??= ex;
                }
            }
        };
        using var playback = new WasapiOut(endpoint, AudioClientShareMode.Shared, true, 75);
        using var tone = new RawSourceWaveStream(new MemoryStream(audio, writable: false), format);
        playback.Init(tone);
        try
        {
            capture.StartRecording();
            playback.Play();
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
        finally
        {
            playback.Stop();
            capture.StopRecording();
            Thread.Sleep(150); // permit last loopback callback to complete
        }
        if (captureError is not null)
            throw new IOException("Virtual endpoint capture stopped with an error.", captureError);
        if (callbackError is not null)
            throw new IOException("Invalid virtual endpoint PCM packet.", callbackError);
        LoopbackToneVerifier.ToneEvidence evidence;
        lock (sync) evidence = verifier.Evaluate();
        Console.WriteLine(
            $"Virtual PCM proof: frames={evidence.Frames} RMS={evidence.Rms:F5} " +
            $"peak={evidence.Peak:F5} matched440={evidence.Matched440Amplitude:F5} " +
            $"coherence={evidence.Coherence:F3}");
        if (!evidence.Passed)
        {
            Console.Error.WriteLine(
                "FAIL: no matching 440 Hz tone in the virtual endpoint loopback. " +
                "Other non-silent audio/DC does not qualify as proof.");
            return 3;
        }
        Console.WriteLine(
            "PASS: loopback PCM contained the specific generated 440 Hz tone " +
            "(frequency-matched, phase-insensitive). " +
            "This proves only the isolated LAB virtual endpoint signal. " +
            "Physical playback and other applications remain unverified.");
        return 0;
    }

    private static string? GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i < 0 || i >= args.Length - 1 ? null : args[i + 1];
    }

    // In-memory, hardware-free proof that real PCM16 packets can flow through
    // our native C++ DSP into the physical output's independent float32 rate.
    // This does NOT prove that an installed virtual driver supplies OS PCM.
    private static int CheckPipeline()
    {
        const int sourceRate = 44100;
        const int outputRate = 48000;
        const int channels = 2;
        const int framesPerPacket = 441;
        const int packetCount = 10;
        var adapter = new PcmFormatAdapter(
            new WaveFormat(sourceRate, 16, channels),
            WaveFormat.CreateIeeeFloatWaveFormat(outputRate, channels));
        using var engine = new NativeDsp(sourceRate, channels, 150);
        long framesOut = 0;
        double power = 0;
        long measuredSamples = 0;
        for (int packet = 0; packet < packetCount; packet++)
        {
            var input = new byte[framesPerPacket * channels * sizeof(short)];
            for (int i = 0; i < framesPerPacket; i++)
            {
                var frame = packet * framesPerPacket + i;
                var sample = (short)MathF.Round(0.1f * 32767f *
                    MathF.Sin(2f * MathF.PI * 440f * frame / sourceRate));
                for (int channel = 0; channel < channels; channel++)
                    BinaryPrimitives.WriteInt16LittleEndian(
                        input.AsSpan((i * channels + channel) * sizeof(short)),
                        sample);
            }

            byte[] dspInput = adapter.Decode(input, input.Length);
            engine.Process(dspInput, dspInput.Length, channels);
            byte[] physicalPcm = adapter.Encode(dspInput, dspInput.Length);
            if (physicalPcm.Length % (channels * sizeof(float)) != 0)
                throw new InvalidOperationException("Native pipeline lost output frame alignment.");
            framesOut += physicalPcm.Length / (channels * sizeof(float));
            if (packet < 2) continue; // Skip gain-smoother startup for RMS check.
            for (int offset = 0; offset < physicalPcm.Length; offset += sizeof(float))
            {
                float value = BinaryPrimitives.ReadSingleLittleEndian(
                    physicalPcm.AsSpan(offset));
                if (!float.IsFinite(value) || Math.Abs(value) > 1f)
                    throw new InvalidOperationException("Invalid native pipeline output sample.");
                power += value * value;
                measuredSamples++;
            }
        }
        double rms = Math.Sqrt(power / Math.Max(1, measuredSamples));
        if (Math.Abs(framesOut - outputRate / 10) > 2 || rms is < 0.095 or > 0.12)
            throw new InvalidOperationException(
                $"Native PCM16→DSP→48k float pipeline failed: frames={framesOut}, RMS={rms:F5}");
        Console.WriteLine(
            $"PASS: own native 150% DSP on PCM16 44.1k stereo → float32 48k; " +
            $"frames={framesOut}, RMS={rms:F5}; no Windows devices accessed.");
        return 0;
    }

    private static int CheckNative()
    {
        const int rate = 48000;
        const int channels = 2;
        // Native DLL smoke test without requiring a Windows audio device.
        using var engine = new NativeDsp(rate, channels, 150);
        byte[] pcm = new byte[rate / 10 * channels * sizeof(float)];
        for (var offset = 0; offset < pcm.Length; offset += sizeof(float))
            BinaryPrimitives.WriteSingleLittleEndian(pcm.AsSpan(offset), 0.1f);
        engine.Process(pcm, pcm.Length, channels);
        float sample = BinaryPrimitives.ReadSingleLittleEndian(pcm.AsSpan(pcm.Length - sizeof(float)));
        if (Math.Abs(sample - 0.15f) > 0.001f)
            throw new InvalidOperationException("Managed-to-native DSP gain test failed.");
        using var eqEngine = new NativeDsp(rate, channels, 100);
        eqEngine.ConfigureEqualizer(true, new float[] { 0, 0, 0, 0, 8, 0, 0 });
        byte[] tone = new byte[rate / 10 * channels * sizeof(float)];
        for (var frame = 0; frame < rate / 10; frame++)
        {
            float value = 0.02f * MathF.Sin(2f * MathF.PI * 1000f * frame / rate);
            for (var channel = 0; channel < channels; channel++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    tone.AsSpan((frame * channels + channel) * sizeof(float)), value);
        }
        eqEngine.Process(tone, tone.Length, channels);
        double energy = 0;
        var count = 0;
        for (var frame = rate / 20; frame < rate / 10; frame++)
        {
            var v = BinaryPrimitives.ReadSingleLittleEndian(
                tone.AsSpan(frame * channels * sizeof(float)));
            energy += v * v;
            count++;
        }
        var level = Math.Sqrt(energy / count);
        if (level < 0.028)
            throw new InvalidOperationException("Managed native 1 kHz EQ response test failed.");
        Console.WriteLine($"Native Windows bridge P/Invoke: gain={sample:F3}, 1kHz EQ RMS={level:F4}; passed");
        return 0;
    }
}
