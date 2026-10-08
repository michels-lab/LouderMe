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

            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            if (args.Contains("--list-devices"))
            {
                foreach (var device in devices)
                    Console.WriteLine($"[{device.FriendlyName}] {device.ID}");
                return 0;
            }

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
            if (!IsFloat32(inputFormat) || !IsFloat32(outputFormat) ||
                inputFormat.SampleRate != outputFormat.SampleRate ||
                inputFormat.Channels != outputFormat.Channels ||
                inputFormat.Channels is < 1 or > 8)
            {
                throw new NotSupportedException(
                    $"No automatic conversion yet. Require matching float32 mix format. " +
                    $"Virtual: {inputFormat}; Physical: {outputFormat}");
            }

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
            var buffer = new BufferedWaveProvider(inputFormat, TimeSpan.FromMilliseconds(250))
            {
                DiscardOnBufferOverflow = false,
                ReadFully = true
            };
            playback.Init(buffer);

            long processedFrames = 0;
            long droppedFrames = 0;
            var recordingStopped = new ManualResetEventSlim(false);
            Exception? recordingError = null;
            capture.RecordingStopped += (_, e) =>
            {
                recordingError = e.Exception;
                recordingStopped.Set();
            };
            capture.DataAvailable += (_, e) =>
            {
                var frameBytes = inputFormat.BlockAlign;
                if (frameBytes == 0 || e.BytesRecorded % frameBytes != 0) return;
                var frameCount = e.BytesRecorded / frameBytes;
                if (buffer.BufferLength - buffer.BufferedBytes < e.BytesRecorded)
                {
                    Interlocked.Add(ref droppedFrames, frameCount);
                    return;
                }
                engine.Process(e.Buffer, e.BytesRecorded, inputFormat.Channels);
                buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                Interlocked.Add(ref processedFrames, frameCount);
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
                Console.WriteLine("Press Ctrl+C to stop; this is not a shipped system driver.");

                playback.Play();
                capture.StartRecording();
                while (!shutdown.Wait(1000))
                {
                    if (recordingStopped.IsSet)
                    {
                        if (recordingError is not null) throw recordingError;
                        throw new IOException("Virtual capture stopped unexpectedly.");
                    }
                    if (playback.PlaybackState != PlaybackState.Playing)
                        throw new IOException("Physical audio playback stopped.");
                    Console.WriteLine($"frames={Interlocked.Read(ref processedFrames)} " +
                        $"dropped={Interlocked.Read(ref droppedFrames)} " +
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

    private static string? GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i < 0 || i >= args.Length - 1 ? null : args[i + 1];
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
