using System.Buffers.Binary;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Non-recurring, quiet 440 Hz lab stimulus routed ONLY into the selected
/// LouderME virtual render endpoint. Not an operating-system driver.
/// </summary>
internal static class PhysicalRouteTone
{
    internal const float Amplitude = 0.025f;
    internal const int DurationSeconds = 2;

    internal static byte[] Create(WaveFormat format)
    {
        if (!PcmFormatAdapter.IsSupported(format) ||
            format.Channels is < 1 or > 2)
            throw new NotSupportedException(
                "Isolated physical-route proof supports PCM16/float32 mono/stereo only.");
        int frames = checked(format.SampleRate * DurationSeconds);
        int channels = format.Channels;
        var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, channels);
        var pcm = new byte[checked(frames * channels * sizeof(float))];
        for (int frame = 0; frame < frames; frame++)
        {
            float value = Amplitude * MathF.Sin(
                2 * MathF.PI * LoopbackToneVerifier.FrequencyHz *
                frame / format.SampleRate);
            for (int channel = 0; channel < channels; channel++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    pcm.AsSpan((frame * channels + channel) * sizeof(float)), value);
        }

        // The selected render endpoint may use PCM16, not just IEEE float.
        // The conversion path is the same numerically tested adapter that
        // feeds the native DSP and physical playback endpoint.
        var encoder = new PcmFormatAdapter(floatFormat, format);
        return encoder.Encode(pcm, pcm.Length);
    }

    internal static void RunSelfTests()
    {
        foreach (int rate in new[] { 44100, 48000, 96000 })
        foreach (int channels in new[] { 1, 2 })
        foreach (int bits in new[] { 16, 32 })
        {
            var format = bits == 16
                ? new WaveFormat(rate, 16, channels)
                : WaveFormat.CreateIeeeFloatWaveFormat(rate, channels);
            byte[] stimulus = Create(format);
            if (stimulus.Length != rate * DurationSeconds * format.BlockAlign)
                throw new InvalidOperationException("Lab stimulus frames are missing.");
            var decoder = new PcmFormatAdapter(
                format, WaveFormat.CreateIeeeFloatWaveFormat(rate, channels));
            byte[] signal = decoder.Decode(stimulus, stimulus.Length);
            var verifier = new LoopbackToneVerifier(
                WaveFormat.CreateIeeeFloatWaveFormat(rate, channels));
            const int maxFramesPerPacket = 217;
            int packetBytes = maxFramesPerPacket * channels * sizeof(float);
            for (int index = 0; index < signal.Length; index += packetBytes)
                verifier.Add(signal.AsSpan(index, Math.Min(packetBytes, signal.Length - index)));
            var proof = verifier.Evaluate();
            if (!proof.Passed || proof.Coherence < 0.95 ||
                proof.Matched440Amplitude is < 0.022 or > 0.027)
                throw new InvalidOperationException(
                    $"Lab tone invalid for {rate} Hz, {channels}ch, {bits}-bit: {proof}");
        }
        Console.WriteLine("PASS: isolated lab tone generation PCM16/float32 " +
            "mono/stereo 44.1/48/96 kHz, fragmented 440 Hz identity.");
    }
}
