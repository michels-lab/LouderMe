using System.Buffers.Binary;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LouderMeIndependentTone;

/// <summary>
/// Intentionally separate Windows process and project from LouderMeAudioBridge.
/// Plays a low-level, exact 440 Hz signal to one explicit virtual endpoint.
/// This tool contains NO DSP, no physical speaker output, no default-device
/// switch, no driver installation, microphone capture, files or networking.
/// </summary>
internal static class Program
{
    private static readonly Guid FloatSubtype =
        new("00000003-0000-0010-8000-00aa00389b71");
    private static readonly Guid PcmSubtype =
        new("00000001-0000-0010-8000-00aa00389b71");

    internal const int Frequency = 440;
    internal const float Amplitude = 0.025f;
    internal const int DurationSeconds = 2;

    private static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--check-tone"))
            {
                TestTone();
                return 0;
            }
            if (!args.Contains("--confirm-experimental") ||
                !args.Contains("--external-app-source"))
                throw new ArgumentException(
                    "Use --device-id <EXACT_VIRTUAL_RENDER_ID> " +
                    "--external-app-source --confirm-experimental (isolated VM only).");
            var index = Array.IndexOf(args, "--device-id");
            if (index < 0 || index + 1 >= args.Length ||
                string.IsNullOrWhiteSpace(args[index + 1]))
                throw new ArgumentException("An exact explicit virtual endpoint ID is required.");
            var id = args[index + 1];
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var device = devices.SingleOrDefault(x => x.ID == id)
                ?? throw new InvalidOperationException("Selected virtual endpoint is not active.");
            if (!device.FriendlyName.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Independent tone may play ONLY into LouderMe's named virtual render endpoint.");
            var format = device.AudioClient.MixFormat;
            var pcm = CreateTone(format);
            using var reader = new RawSourceWaveStream(
                new MemoryStream(pcm, writable: false), format);
            using var playback = new WasapiOut(device, AudioClientShareMode.Shared, true, 75);
            using var complete = new ManualResetEventSlim(false);
            Exception? playbackError = null;
            playback.PlaybackStopped += (_, e) =>
            {
                playbackError = e.Exception;
                complete.Set();
            };
            playback.Init(reader);
            playback.Play();
            if (!complete.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("Separate application did not finish the 2 s tone.");
            if (playbackError is not null)
                throw new IOException("Independent application audio endpoint failed.", playbackError);
            Console.WriteLine(
                $"EXTERNAL_APP_TONE_FINISHED 440Hz frames={format.SampleRate * DurationSeconds} " +
                $"channels={format.Channels}; physical loopback is verified only by the separate bridge.");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("EXTERNAL_APP_TONE_FAILED " + e.Message);
            return 4;
        }
    }

    internal static byte[] CreateTone(WaveFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        bool isPcm16 = format.BitsPerSample == 16 &&
            (format.Encoding == WaveFormatEncoding.Pcm ||
             format is WaveFormatExtensible e1 && e1.SubFormat == PcmSubtype);
        bool isPcm24 = format.BitsPerSample == 24 &&
            (format.Encoding == WaveFormatEncoding.Pcm ||
             format is WaveFormatExtensible e3 && e3.SubFormat == PcmSubtype);
        bool isFloat32 = format.BitsPerSample == 32 &&
            (format.Encoding == WaveFormatEncoding.IeeeFloat ||
             format is WaveFormatExtensible e2 && e2.SubFormat == FloatSubtype);
        if ((!isPcm16 && !isPcm24 && !isFloat32) ||
            format.SampleRate is < 8000 or > 192000 ||
            format.Channels is < 1 or > 2 ||
            format.BlockAlign != format.Channels * format.BitsPerSample / 8)
            throw new NotSupportedException(
                "External app lab tone requires PCM16/PCM24/float32 mono/stereo 8–192 kHz.");
        var frames = checked(format.SampleRate * DurationSeconds);
        var pcm = new byte[checked(frames * format.BlockAlign)];
        for (int frame = 0; frame < frames; frame++)
        {
            float v = Amplitude * MathF.Sin(
                2f * MathF.PI * Frequency * frame / format.SampleRate);
            for (int channel = 0; channel < format.Channels; channel++)
            {
                var dest = pcm.AsSpan(frame * format.BlockAlign +
                    channel * (format.BitsPerSample / 8));
                if (isFloat32)
                    BinaryPrimitives.WriteSingleLittleEndian(dest, v);
                else if (isPcm16)
                    BinaryPrimitives.WriteInt16LittleEndian(
                        dest, (short)MathF.Round(v * 32767f));
                else
                {
                    int signed24 = (int)MathF.Round(v * 8388607f);
                    dest[0] = (byte)signed24;
                    dest[1] = (byte)(signed24 >> 8);
                    dest[2] = (byte)(signed24 >> 16);
                }
            }
        }
        return pcm;
    }

    private static void TestTone()
    {
        foreach (int rate in new[] { 44100, 48000, 96000 })
        foreach (int channels in new[] { 1, 2 })
        foreach (int bits in new[] { 16, 24, 32 })
        {
            var format = bits == 32 ? WaveFormat.CreateIeeeFloatWaveFormat(rate, channels)
                : new WaveFormat(rate, bits, channels);
            byte[] pcm = CreateTone(format);
            if (pcm.Length != rate * DurationSeconds * format.BlockAlign)
                throw new Exception("Independent application tone has invalid frame count.");
            double energy = 0, projectionSin = 0, projectionCos = 0;
            for (int frame = 0; frame < rate * DurationSeconds; frame++)
            {
                int at = frame * format.BlockAlign;
                double sample;
                if (bits == 16)
                    sample = BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(at)) / 32768.0;
                else if (bits == 24)
                {
                    int packed24 = pcm[at] | (pcm[at + 1] << 8) |
                        (pcm[at + 2] << 16);
                    if ((packed24 & 0x800000) != 0)
                        packed24 |= unchecked((int)0xFF000000);
                    sample = packed24 / 8388608.0;
                }
                else
                    sample = BinaryPrimitives.ReadSingleLittleEndian(pcm.AsSpan(at));
                if (!double.IsFinite(sample) || Math.Abs(sample) > 0.03)
                    throw new Exception("Independent application tone amplitude invalid.");
                var angle = 2 * Math.PI * Frequency * frame / rate;
                projectionSin += sample * Math.Sin(angle);
                projectionCos += sample * Math.Cos(angle);
                energy += sample * sample;
            }
            var amplitude = 2 * Math.Sqrt(
                projectionSin * projectionSin + projectionCos * projectionCos) /
                (rate * DurationSeconds);
            var rms = Math.Sqrt(energy / (rate * DurationSeconds));
            if (amplitude is < .024 or > .026 || rms is < .016 or > .019)
                throw new Exception("Independent application lab tone frequency/RMS invalid.");
        }
        foreach (WaveFormat bad in new[] {
            new WaveFormat(48000, 8, 2),
            new WaveFormat(48000, 32, 2), // Integer PCM32 is not IEEE float.
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 3),
        })
        {
            try { CreateTone(bad); }
            catch (NotSupportedException) { continue; }
            throw new Exception("Unsupported independent app audio format accepted.");
        }
        Console.WriteLine("PASS: separate Windows process 440 Hz source generator PCM16/PCM24/float32, mono/stereo, 44.1/48/96 kHz; no devices.");
    }
}
