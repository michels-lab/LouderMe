using System.Buffers.Binary;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Stateful PCM16/float32 decoder and linear-interpolation format adapter for
/// the *experimental* virtual-to-physical route. Consumed only by the capture
/// callback. This is not a true-peak-quality production resampler.
/// </summary>
internal sealed class PcmFormatAdapter
{
    private static readonly Guid FloatSubtype =
        new("00000003-0000-0010-8000-00aa00389b71");
    private static readonly Guid PcmSubtype =
        new("00000001-0000-0010-8000-00aa00389b71");

    private readonly WaveFormat _input;
    private readonly WaveFormat _output;
    private readonly int _channels;
    private readonly int _outputChannels;
    private readonly bool _inputFloat;
    private readonly bool _outputFloat;
    private readonly double _inputFramesPerOutputFrame;
    private readonly float[] _lastFrame;
    private readonly StreamingDownsampleLowPass? _antiAlias;
    private double _nextPosition;
    private double _rateAdjustmentPpm;
    private bool _hasLastFrame;

    public PcmFormatAdapter(WaveFormat input, WaveFormat output)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        ValidateFormat(input);
        ValidateFormat(output);
        if (input.Channels != output.Channels &&
            !(input.Channels == 1 && output.Channels == 2) &&
            !(input.Channels == 2 && output.Channels == 1))
            throw new NotSupportedException(
                "Only matching channel layouts or explicit mono/stereo conversions are supported.");
        if (input.SampleRate is < 8000 or > 192000 ||
            output.SampleRate is < 8000 or > 192000)
            throw new NotSupportedException("Sample rates must be between 8 kHz and 192 kHz.");
        var ratio = (double)output.SampleRate / input.SampleRate;
        if (ratio is < 0.25 or > 4)
            throw new NotSupportedException(
                "Only source/output rate ratios between 1:4 and 4:1 are supported.");
        _channels = input.Channels;
        _outputChannels = output.Channels;
        _inputFloat = IsFloat(input);
        _outputFloat = IsFloat(output);
        _inputFramesPerOutputFrame = (double)input.SampleRate / output.SampleRate;
        _lastFrame = new float[_channels];
        _antiAlias = output.SampleRate < input.SampleRate
            ? new StreamingDownsampleLowPass(input.SampleRate, output.SampleRate, _channels)
            : null;
    }

    /// <summary>
    /// Small bounded correction for independent source/sink device clocks.
    /// At +ppm the converter emits slightly fewer sink frames; at -ppm
    /// slightly more. Atomic write/read avoids cross-thread data races.
    /// This is not a replacement for a high-quality clock-recovery PLL.
    /// </summary>
    public void SetRateAdjustmentPpm(double ppm)
    {
        if (!double.IsFinite(ppm) ||
            Math.Abs(ppm) > AdaptiveBufferClock.MaximumCorrectionPpm)
            throw new ArgumentOutOfRangeException(nameof(ppm));
        Volatile.Write(ref _rateAdjustmentPpm, ppm);
    }

    public static bool IsSupported(WaveFormat f)
    {
        if (f.Channels is < 1 or > 8) return false;
        if (f.SampleRate is < 8000 or > 192000) return false;
        var isFloat = IsFloat(f);
        var isPcm = IsPcm16(f);
        return (isFloat || isPcm) &&
            f.BlockAlign == f.Channels * (isFloat ? sizeof(float) : sizeof(short));
    }

    private static bool IsFloat(WaveFormat format) =>
        format.BitsPerSample == 32 &&
        (format.Encoding == WaveFormatEncoding.IeeeFloat ||
         (format is WaveFormatExtensible ext && ext.SubFormat == FloatSubtype));

    private static bool IsPcm16(WaveFormat format) =>
        format.BitsPerSample == 16 &&
        (format.Encoding == WaveFormatEncoding.Pcm ||
         (format is WaveFormatExtensible ext && ext.SubFormat == PcmSubtype));

    private static void ValidateFormat(WaveFormat format)
    {
        if (!IsSupported(format))
            throw new NotSupportedException(
                "The experimental route requires PCM16 or IEEE float32, mono to eight channels.");
    }

    /// <summary>Make a private interleaved float32 copy before invoking the native DSP.</summary>
    public byte[] Decode(byte[] source, int sourceBytes)
    {
        if (sourceBytes < 0 || sourceBytes > source.Length ||
            sourceBytes % _input.BlockAlign != 0 || sourceBytes > 4 * 1024 * 1024)
            throw new ArgumentException("Invalid source PCM packet size.", nameof(sourceBytes));
        int samples = checked(sourceBytes / _input.BlockAlign * _channels);
        var decoded = new byte[checked(samples * sizeof(float))];
        for (int i = 0; i < samples; i++)
        {
            var sample = _inputFloat
                ? BinaryPrimitives.ReadSingleLittleEndian(source.AsSpan(i * sizeof(float)))
                : BinaryPrimitives.ReadInt16LittleEndian(source.AsSpan(i * sizeof(short))) / 32768f;
            if (!float.IsFinite(sample))
                throw new InvalidDataException("Nonfinite input PCM.");
            BinaryPrimitives.WriteSingleLittleEndian(decoded.AsSpan(i * sizeof(float)), sample);
        }
        return decoded;
    }

    /// <summary>
    /// Encode DSP output for the chosen physical mixer, retaining fractional
    /// sample position and the last source frame across callback boundaries.
    /// An unpaired final frame is deliberately not extrapolated during a rate
    /// change; no click-prone artificial padding is inserted. Downsampling
    /// applies a streaming anti-alias FIR in-place to the caller-owned PCM
    /// buffer before interpolation; input buffers must not be reused afterward.
    /// </summary>
    public byte[] Encode(byte[] floatPcm, int byteCount)
    {
        int floatFrameBytes = _channels * sizeof(float);
        if (byteCount < 0 || byteCount > floatPcm.Length ||
            byteCount % floatFrameBytes != 0 || byteCount > 4 * 1024 * 1024)
            throw new ArgumentException("Invalid native DSP packet size.", nameof(byteCount));
        int frames = byteCount / floatFrameBytes;
        if (frames == 0) return Array.Empty<byte>();
        _antiAlias?.ProcessInPlace(floatPcm, byteCount);

        int capacityFrames = checked((int)Math.Ceiling(
            (frames + 2.0) * _output.SampleRate / _input.SampleRate) + 8);
        var output = new byte[checked(capacityFrames * _output.BlockAlign)];
        int emitted = 0;
        // Capture side is the sole owner of Encode; the control loop
        // updates only the atomic correction once per 250 ms.
        double packetStep = _inputFramesPerOutputFrame *
            (1.0 + Volatile.Read(ref _rateAdjustmentPpm) * 0.000001);
        while (_nextPosition <= frames - 1)
        {
            int leftIndex = (int)Math.Floor(_nextPosition);
            if (leftIndex < -1 || (leftIndex < 0 && !_hasLastFrame))
                throw new InvalidOperationException("PCM resampler history is incomplete.");
            float fraction = (float)(_nextPosition - leftIndex);
            // A fractional last-frame sample needs the *next* capture
            // packet's first frame. Defer it instead of throwing or making
            // up padding; _lastFrame bridges the packet boundary.
            if (leftIndex + 1 >= frames && fraction > 0f)
                break;
            for (int channel = 0; channel < _outputChannels; channel++)
            {
                float left = leftIndex < 0
                    ? ReadMappedHistory(channel)
                    : ReadMappedFloat(floatPcm, leftIndex, channel);
                float right = leftIndex + 1 >= frames
                    ? left
                    : ReadMappedFloat(floatPcm, leftIndex + 1, channel);
                float value = left + (right - left) * fraction;
                if (!float.IsFinite(value))
                    throw new InvalidDataException("Nonfinite native DSP output.");
                value = Math.Clamp(value, -1f, 1f);
                int byteOffset = emitted * _output.BlockAlign +
                    channel * (_outputFloat ? sizeof(float) : sizeof(short));
                if (_outputFloat)
                {
                    BinaryPrimitives.WriteSingleLittleEndian(
                        output.AsSpan(byteOffset), value);
                }
                else
                {
                    float scaled = value < 0f ? value * 32768f : value * 32767f;
                    BinaryPrimitives.WriteInt16LittleEndian(
                        output.AsSpan(byteOffset), (short)MathF.Round(scaled));
                }
            }
            emitted++;
            if (emitted > capacityFrames)
                throw new InvalidOperationException("Resampler output capacity exceeded.");
            _nextPosition += packetStep;
        }

        for (int c = 0; c < _channels; c++)
            _lastFrame[c] = ReadFloat(floatPcm, frames - 1, c);
        _hasLastFrame = true;
        _nextPosition -= frames;

        Array.Resize(ref output, emitted * _output.BlockAlign);
        return output;
    }

    private float ReadFloat(byte[] pcm, int frame, int channel) =>
        BinaryPrimitives.ReadSingleLittleEndian(
            pcm.AsSpan((frame * _channels + channel) * sizeof(float)));

    private float ReadMappedHistory(int sinkChannel)
    {
        // Equal-weight stereo → mono preserves symmetric signals and
        // avoids summing two full-scale samples above the valid range.
        if (_channels == 2 && _outputChannels == 1)
            return (_lastFrame[0] + _lastFrame[1]) * 0.5f;
        // Mono → stereo duplicates the signal identically in both ears.
        return _lastFrame[_channels == 1 ? 0 : sinkChannel];
    }

    private float ReadMappedFloat(byte[] pcm, int frame, int sinkChannel)
    {
        if (_channels == 2 && _outputChannels == 1)
            return (ReadFloat(pcm, frame, 0) +
                    ReadFloat(pcm, frame, 1)) * 0.5f;
        return ReadFloat(pcm, frame, _channels == 1 ? 0 : sinkChannel);
    }

    // Deterministic device-free regression tests, exercised by Windows CI.
    public static void RunSelfTests()
    {
        var pcm16 = new WaveFormat(48000, 16, 1);
        var ieee = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
        var mono = new PcmFormatAdapter(pcm16, ieee);
        var input = new byte[8];
        short[] original = [-32768, 0, 16384, 32767];
        for (int i = 0; i < original.Length; i++)
            BinaryPrimitives.WriteInt16LittleEndian(input.AsSpan(i * 2), original[i]);
        byte[] decoded = mono.Decode(input, input.Length);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(decoded.AsSpan(0)), -1f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(decoded.AsSpan(4)), 0f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(decoded.AsSpan(8)), 0.5f);
        byte[] encoded = mono.Encode(decoded, decoded.Length);
        if (encoded.Length != original.Length * sizeof(float))
            throw new InvalidOperationException("PCM16 to float frame count changed.");
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(encoded.AsSpan(8)), 0.5f);

        var quantizer = new PcmFormatAdapter(ieee, pcm16);
        var samples = new byte[4 * sizeof(float)];
        float[] originalFloats = [-1f, -0.5f, 0.5f, 1f];
        for (int i = 0; i < originalFloats.Length; i++)
            BinaryPrimitives.WriteSingleLittleEndian(samples.AsSpan(i * 4), originalFloats[i]);
        var quantized = quantizer.Encode(samples, samples.Length);
        if (quantized.Length != 8 ||
            BinaryPrimitives.ReadInt16LittleEndian(quantized.AsSpan(0)) != -32768 ||
            BinaryPrimitives.ReadInt16LittleEndian(quantized.AsSpan(6)) != 32767)
            throw new InvalidOperationException("Float to PCM16 saturation failed.");

        TestStreamingResample(48000, 44100, 480, 2);
        TestStreamingResample(44100, 48000, 441, 2);
        TestStreamingResample(48000, 96000, 480, 2);
        TestStreamingResample(96000, 48000, 960, 2);

        TestMonoStereoMapping();
        TestShortPacketBoundaries(44100, 48000);
        TestShortPacketBoundaries(48000, 44100);
        TestShortPacketBoundaries(48000, 96000);
        TestDownsampleAntiAlias(96000, 48000, 30000);
        TestDownsampleAntiAlias(48000, 44100, 23500);
        TestDownsampleChannelIsolation();
        TestAdjustableRateContinuity();

        try
        {
            _ = new PcmFormatAdapter(
                WaveFormat.CreateIeeeFloatWaveFormat(48000, 6),
                WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            throw new InvalidOperationException("Unsupported 5.1 to stereo mapping was accepted.");
        }
        catch (NotSupportedException) { }

        Console.WriteLine(
            "PASS: PCM16/float32 conversion, saturation, 44.1/48/96 kHz " +
            "streaming, short-packet interpolation, mono/stereo mapping, " +
            "windowed-sinc anti-alias rejection at 96->48 and 48->44.1 kHz, " +
            "channel isolation, bounded clock adjustment and unsupported 5.1 rejection.");
    }

    private static void TestMonoStereoMapping()
    {
        var one = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
        var two = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var mono = new byte[3 * sizeof(float)];
        float[] values = [0.25f, -0.5f, 0.75f];
        for (int f = 0; f < values.Length; f++)
            BinaryPrimitives.WriteSingleLittleEndian(mono.AsSpan(f * 4), values[f]);

        var up = new PcmFormatAdapter(one, two);
        byte[] stereo = up.Encode(mono, mono.Length);
        if (stereo.Length != 3 * 2 * sizeof(float))
            throw new InvalidOperationException("Mono/stereo upmix lost output frames.");
        for (int f = 0; f < values.Length; f++)
        {
            AssertClose(BinaryPrimitives.ReadSingleLittleEndian(stereo.AsSpan((f * 2) * 4)), values[f]);
            AssertClose(BinaryPrimitives.ReadSingleLittleEndian(stereo.AsSpan((f * 2 + 1) * 4)), values[f]);
        }

        // Opposite-phase stereo should cancel on explicitly selected mono
        // output; no undocumented per-channel selection or overflow.
        var outMono = new PcmFormatAdapter(two, one);
        var input = new byte[4 * 2 * sizeof(float)];
        float[] pairs = [0.8f, -0.2f, -0.5f, 0.5f, 0.75f, -0.75f, 1f, 1f];
        for (int i = 0; i < pairs.Length; i++)
            BinaryPrimitives.WriteSingleLittleEndian(input.AsSpan(i * 4), pairs[i]);
        byte[] down = outMono.Encode(input, input.Length);
        if (down.Length != 4 * sizeof(float))
            throw new InvalidOperationException("Stereo-to-mono lost output frames.");
        float[] expected = [0.3f, 0f, 0f, 1f];
        for (int f = 0; f < expected.Length; f++)
            AssertClose(BinaryPrimitives.ReadSingleLittleEndian(down.AsSpan(f * 4)), expected[f]);
    }

    private static void TestShortPacketBoundaries(int sourceRate, int outputRate)
    {
        var adapter = new PcmFormatAdapter(
            WaveFormat.CreateIeeeFloatWaveFormat(sourceRate, 2),
            WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 2));
        const int totalFrames = 4000;
        int[] fragments = [1, 3, 7, 2, 511, 1, 89, 4, 32, 1, 1001, 13];
        int current = 0;
        int packet = 0;
        long emitted = 0;
        float previous = 0;
        bool hasPrevious = false;
        while (current < totalFrames)
        {
            int count = Math.Min(fragments[packet++ % fragments.Length], totalFrames - current);
            var pcm = new byte[count * 2 * sizeof(float)];
            for (int frame = 0; frame < count; frame++)
            {
                float signal = 0.2f * MathF.Sin(2f * MathF.PI * 1000 *
                    (current + frame) / sourceRate);
                for (int ch = 0; ch < 2; ch++)
                    BinaryPrimitives.WriteSingleLittleEndian(
                        pcm.AsSpan((frame * 2 + ch) * sizeof(float)), signal);
            }
            var output = adapter.Encode(pcm, pcm.Length);
            if (output.Length % (2 * sizeof(float)) != 0)
                throw new InvalidOperationException("Short-packet output misaligned.");
            emitted += output.Length / (2 * sizeof(float));
            for (int index = 0; index < output.Length; index += 2 * sizeof(float))
            {
                float value = BinaryPrimitives.ReadSingleLittleEndian(output.AsSpan(index));
                float other = BinaryPrimitives.ReadSingleLittleEndian(output.AsSpan(index + 4));
                if (!float.IsFinite(value) || Math.Abs(value) > 0.201f)
                    throw new InvalidOperationException("Short-packet resampling created invalid signal.");
                AssertClose(other, value);
                if (hasPrevious && Math.Abs(value - previous) > 0.065f)
                    throw new InvalidOperationException("Short-packet resampling introduced a PCM discontinuity.");
                hasPrevious = true;
                previous = value;
            }
            current += count;
        }

        double expectedFrames = (double)totalFrames * outputRate / sourceRate;
        if (Math.Abs(emitted - expectedFrames) > 2)
            throw new InvalidOperationException(
                $"Short-packet rate conversion lost frames: {emitted} vs {expectedFrames:F1}.");
    }

    // Compare the same deterministic sample path at a preserved 1 kHz tone
    // and a tone ABOVE the output Nyquist that would otherwise alias down.
    // Exclude the FIR's start-up transient before computing input/output RMS.
    private static void TestDownsampleAntiAlias(int inputRate, int outputRate,
        int rejectedToneHz)
    {
        double preserved = MeasureDownsampleRms(inputRate, outputRate, 1000);
        double rejected = MeasureDownsampleRms(inputRate, outputRate, rejectedToneHz);
        if (preserved < 0.117 || preserved > 0.158)
            throw new InvalidOperationException(
                $"Anti-alias filter damaged 1 kHz passband {inputRate}->{outputRate}: RMS={preserved:F6}");
        if (rejected > preserved * 0.08)
            throw new InvalidOperationException(
                $"Aliasing rejection too weak at {rejectedToneHz} Hz, " +
                $"{inputRate}->{outputRate}: stop={rejected:F6}, pass={preserved:F6}.");
    }

    private static double MeasureDownsampleRms(int inputRate, int outputRate, int toneHz)
    {
        var adapter = new PcmFormatAdapter(
            WaveFormat.CreateIeeeFloatWaveFormat(inputRate, 2),
            WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 2));
        const int totalFrames = 14000;
        long outputFrames = 0;
        double sumSquares = 0;
        long analyzedFrames = 0;
        int sourceIndex = 0;
        int[] packets = [1, 257, 5, 541, 7, 89, 1001, 3];
        int packetIndex = 0;
        while (sourceIndex < totalFrames)
        {
            int frames = Math.Min(
                packets[packetIndex++ % packets.Length], totalFrames - sourceIndex);
            byte[] source = new byte[frames * 2 * sizeof(float)];
            for (int f = 0; f < frames; f++)
            {
                float x = 0.2f * MathF.Sin(2f * MathF.PI * toneHz *
                    (sourceIndex + f) / inputRate);
                for (int ch = 0; ch < 2; ch++)
                    BinaryPrimitives.WriteSingleLittleEndian(
                        source.AsSpan((f * 2 + ch) * sizeof(float)), x);
            }
            byte[] output = adapter.Encode(source, source.Length);
            int outputCount = output.Length / (2 * sizeof(float));
            for (int f = 0; f < outputCount; f++)
            {
                float x = BinaryPrimitives.ReadSingleLittleEndian(
                    output.AsSpan((f * 2) * sizeof(float)));
                if (!float.IsFinite(x))
                    throw new InvalidOperationException("Anti-alias emitted nonfinite PCM.");
                if (outputFrames + f > 1024)
                {
                    sumSquares += x * x;
                    analyzedFrames++;
                }
            }
            outputFrames += outputCount;
            sourceIndex += frames;
        }

        double expected = (double)totalFrames * outputRate / inputRate;
        if (Math.Abs(outputFrames - expected) > 2 || analyzedFrames < 500)
            throw new InvalidOperationException("Anti-alias streaming frame count error.");
        return Math.Sqrt(sumSquares / analyzedFrames);
    }

    private static void TestDownsampleChannelIsolation()
    {
        const int inputRate = 96000;
        const int outputRate = 48000;
        var adapter = new PcmFormatAdapter(
            WaveFormat.CreateIeeeFloatWaveFormat(inputRate, 2),
            WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 2));
        double leftPower = 0;
        double rightPower = 0;
        for (int packet = 0; packet < 50; packet++)
        {
            const int frames = 127;
            byte[] pcm = new byte[frames * 2 * sizeof(float)];
            for (int f = 0; f < frames; f++)
            {
                float tone = 0.2f * MathF.Sin(2f * MathF.PI * 1000 *
                    (packet * frames + f) / inputRate);
                BinaryPrimitives.WriteSingleLittleEndian(
                    pcm.AsSpan(f * 2 * sizeof(float)), tone);
                // Right channel stays exactly zero.
            }
            byte[] encoded = adapter.Encode(pcm, pcm.Length);
            if (packet < 8) continue; // zero-padded FIR warmup
            for (int byteOffset = 0; byteOffset < encoded.Length; byteOffset += 8)
            {
                float left = BinaryPrimitives.ReadSingleLittleEndian(
                    encoded.AsSpan(byteOffset));
                float right = BinaryPrimitives.ReadSingleLittleEndian(
                    encoded.AsSpan(byteOffset + 4));
                leftPower += left * left;
                rightPower += right * right;
            }
        }
        if (leftPower < 1 || rightPower > 1e-10)
            throw new InvalidOperationException(
                "Streaming anti-alias filter leaked audio across channels.");
    }

    private static void TestAdjustableRateContinuity()
    {
        const int rate = 48000;
        var format = WaveFormat.CreateIeeeFloatWaveFormat(rate, 2);
        foreach (double ppm in new[] { -250.0, 0.0, 250.0 })
        {
            var a = new PcmFormatAdapter(format, format);
            a.SetRateAdjustmentPpm(ppm);
            long outputFrames = 0;
            float previous = 0;
            bool hasPrevious = false;
            for (int packet = 0; packet < 300; packet++)
            {
                int frames = packet % 3 == 0 ? 1 : packet % 3 == 1 ? 11 : 512;
                byte[] pcm = new byte[frames * 8];
                int priorFrame = packet / 3 * (1 + 11 + 512) +
                    (packet % 3 == 0 ? 0 : packet % 3 == 1 ? 1 : 12);
                for (int f = 0; f < frames; f++)
                {
                    float tone = 0.2f * MathF.Sin(
                        2 * MathF.PI * 1000 * (priorFrame + f) / rate);
                    for (int ch = 0; ch < 2; ch++)
                        BinaryPrimitives.WriteSingleLittleEndian(
                            pcm.AsSpan((f * 2 + ch) * 4), tone);
                }
                byte[] converted = a.Encode(pcm, pcm.Length);
                for (int pos = 0; pos < converted.Length; pos += 8)
                {
                    float sample = BinaryPrimitives.ReadSingleLittleEndian(
                        converted.AsSpan(pos));
                    float other = BinaryPrimitives.ReadSingleLittleEndian(
                        converted.AsSpan(pos + 4));
                    AssertClose(sample, other);
                    if (hasPrevious && Math.Abs(sample - previous) > 0.07)
                        throw new InvalidOperationException(
                            "Rate correction broke streaming sample continuity.");
                    hasPrevious = true;
                    previous = sample;
                }
                outputFrames += converted.Length / 8;
            }
            const int sourceFrames = 100 * (1 + 11 + 512);
            double expected = sourceFrames / (1.0 + ppm * 0.000001);
            if (Math.Abs(outputFrames - expected) > 2)
                throw new InvalidOperationException(
                    "Bounded ppm correction changed sample count unexpectedly.");
        }

        try
        {
            new PcmFormatAdapter(format, format).SetRateAdjustmentPpm(500);
            throw new InvalidOperationException("Unsafe clock correction was accepted.");
        }
        catch (ArgumentOutOfRangeException) { }
    }

    private static void TestStreamingResample(int sourceRate, int outputRate,
        int framesPerChunk, int chunks)
    {
        var adapter = new PcmFormatAdapter(
            WaveFormat.CreateIeeeFloatWaveFormat(sourceRate, 2),
            WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 2));
        long written = 0;
        var source = new byte[framesPerChunk * 2 * sizeof(float)];
        for (int chunk = 0; chunk < chunks; chunk++)
        {
            for (int f = 0; f < framesPerChunk; f++)
            {
                float sample = 0.2f * MathF.Sin(
                    2f * MathF.PI * 1000f * (chunk * framesPerChunk + f) / sourceRate);
                for (int c = 0; c < 2; c++)
                    BinaryPrimitives.WriteSingleLittleEndian(
                        source.AsSpan((f * 2 + c) * sizeof(float)), sample);
            }
            byte[] output = adapter.Encode(source, source.Length);
            if (output.Length % (2 * sizeof(float)) != 0)
                throw new InvalidOperationException("Resampled frames not aligned.");
            written += output.Length / (2 * sizeof(float));
            foreach (int offset in Enumerable.Range(0, output.Length / sizeof(float)))
            {
                float sample = BinaryPrimitives.ReadSingleLittleEndian(
                    output.AsSpan(offset * sizeof(float)));
                if (!float.IsFinite(sample) || Math.Abs(sample) > 0.21f)
                    throw new InvalidOperationException("Resampler produced bad amplitude.");
            }
        }
        double expected = (double)framesPerChunk * chunks * outputRate / sourceRate;
        if (Math.Abs(written - expected) > 2)
            throw new InvalidOperationException(
                "Streaming resampler lost or duplicated unexpected frames.");
    }

    private static void AssertClose(float actual, float expected)
    {
        if (MathF.Abs(actual - expected) > 0.001f)
            throw new InvalidOperationException("PCM numeric regression failure.");
    }
}
