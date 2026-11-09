using System.Buffers.Binary;
using System.Numerics;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Stateful PCM16/PCM24/float32 decoder and linear-interpolation format adapter for
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
    // Precomputed once per route. Null outside explicit 5.1/7.1 -> stereo.
    private readonly float[]? _surroundLeft;
    private readonly float[]? _surroundRight;
    private double _nextPosition;
    private double _rateAdjustmentPpm;
    private bool _hasLastFrame;

    public PcmFormatAdapter(WaveFormat input, WaveFormat output)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        ValidateFormat(input);
        ValidateFormat(output);
        bool downmixSurround = input.Channels is 6 or 8 && output.Channels == 2;
        if (input.Channels != output.Channels &&
            !(input.Channels == 1 && output.Channels == 2) &&
            !(input.Channels == 2 && output.Channels == 1) &&
            !downmixSurround)
            throw new NotSupportedException(
                "Only matching channels, mono/stereo, or explicit 5.1/7.1 to stereo conversion are supported.");
        if (downmixSurround)
        {
            if (input is not WaveFormatExtensible surround ||
                (surround.ChannelMask != 0x3f && surround.ChannelMask != 0x60f &&
                 surround.ChannelMask != 0x63f) ||
                (input.Channels == 6 && surround.ChannelMask == 0x63f) ||
                (input.Channels == 8 && surround.ChannelMask != 0x63f) ||
                (output is WaveFormatExtensible stereo &&
                 stereo.ChannelMask != 0x3))
                throw new NotSupportedException(
                    "Surround downmix requires an explicit standard 5.1/7.1 speaker mask and a front-left/right stereo sink.");
            (_surroundLeft, _surroundRight) = BuildSurroundStereoMatrix(
                surround.ChannelMask, input.Channels);
        }
        // Frame-interleaving alone does not identify which speaker owns each
        // channel. Reject mismatched explicit speaker masks before any PCM moves.
        if (input.Channels == output.Channels &&
            input is WaveFormatExtensible inputLayout &&
            output is WaveFormatExtensible outputLayout &&
            inputLayout.ChannelMask != 0 && outputLayout.ChannelMask != 0 &&
            inputLayout.ChannelMask != outputLayout.ChannelMask)
            throw new NotSupportedException(
                "Source and sink speaker masks differ; raw PCM channel ordering is unsafe.");
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
        if (f is null || f.Channels is < 1 or > 8) return false;
        if (f.SampleRate is < 8000 or > 192000) return false;
        // Windows also permits 24 valid PCM bits LEFT-aligned in 32 bits.
        // Reject every other partial-valid-bit format, including float32.
        bool isPcm24In32 = IsPcm24In32(f);
        if (f is WaveFormatExtensible ext)
        {
            if (ext.ValidBitsPerSample != f.BitsPerSample && !isPcm24In32)
                return false;
            if (ext.ChannelMask != 0 &&
                BitOperations.PopCount(unchecked((uint)ext.ChannelMask)) != f.Channels)
                return false;
            if (f.Channels > 2 && ext.ChannelMask == 0) return false;
        }
        else if (f.Channels > 2) return false;
        var isFloat = IsFloat(f);
        var isPcm = IsPcm16(f) || IsPcm24(f) || isPcm24In32;
        return (isFloat || isPcm) &&
            f.BlockAlign == f.Channels * (f.BitsPerSample / 8);
    }

    internal static bool IsFloat(WaveFormat format) =>
        format.BitsPerSample == 32 &&
        (format.Encoding == WaveFormatEncoding.IeeeFloat ||
         (format is WaveFormatExtensible ext && ext.SubFormat == FloatSubtype));

    private static bool IsPcm16(WaveFormat format) =>
        format.BitsPerSample == 16 &&
        (format.Encoding == WaveFormatEncoding.Pcm ||
         (format is WaveFormatExtensible ext && ext.SubFormat == PcmSubtype));

    private static bool IsPcm24(WaveFormat format) =>
        format.BitsPerSample == 24 &&
        (format.Encoding == WaveFormatEncoding.Pcm ||
         (format is WaveFormatExtensible ext && ext.SubFormat == PcmSubtype));

    private static bool IsPcm24In32(WaveFormat format) =>
        format is WaveFormatExtensible ext &&
        format.BitsPerSample == 32 &&
        ext.ValidBitsPerSample == 24 &&
        ext.SubFormat == PcmSubtype;

    private static void ValidateFormat(WaveFormat format)
    {
        if (!IsSupported(format))
            throw new NotSupportedException(
                "The experimental route requires PCM16, packed PCM24, left-aligned PCM24/32 or IEEE float32, mono to eight channels.");
    }

    /// <summary>Decode into a caller-owned scratch buffer. No allocation per packet.</summary>
    public int DecodeInto(byte[] source, int sourceBytes, Span<byte> destination)
    {
        if (source is null || sourceBytes < 0 || sourceBytes > source.Length ||
            sourceBytes % _input.BlockAlign != 0 || sourceBytes > 4 * 1024 * 1024)
            throw new ArgumentException("Invalid source PCM packet size.", nameof(sourceBytes));
        int samples = checked(sourceBytes / _input.BlockAlign * _channels);
        int bytesNeeded = checked(samples * sizeof(float));
        if (destination.Length < bytesNeeded)
            throw new ArgumentException("Preallocated PCM decode scratch too small.", nameof(destination));
        int sampleBytes = _input.BitsPerSample / 8;
        for (int i = 0; i < samples; i++)
        {
            int offset = i * sampleBytes;
            float sample;
            if (_inputFloat)
                sample = BinaryPrimitives.ReadSingleLittleEndian(source.AsSpan(offset));
            else if (sampleBytes == sizeof(short))
                sample = BinaryPrimitives.ReadInt16LittleEndian(source.AsSpan(offset)) / 32768f;
            else if (sampleBytes == sizeof(int))
            {
                // Windows WAVEFORMATEXTENSIBLE uses LEFT-aligned valid bits.
                int aligned = BinaryPrimitives.ReadInt32LittleEndian(source.AsSpan(offset));
                if ((aligned & 0xff) != 0)
                    throw new InvalidDataException(
                        "PCM24-in-32 contains nonzero low padding bits.");
                sample = (aligned >> 8) / 8388608f;
            }
            else
            {
                // Packed signed little-endian 24-bit PCM: extend bit 23,
                // preserving full-scale negative values without allocation.
                int value = source[offset] | (source[offset + 1] << 8) |
                    (source[offset + 2] << 16);
                if ((value & 0x800000) != 0)
                    value |= unchecked((int)0xFF000000);
                sample = value / 8388608f;
            }
            if (!float.IsFinite(sample))
                throw new InvalidDataException("Nonfinite input PCM.");
            BinaryPrimitives.WriteSingleLittleEndian(destination.Slice(i * sizeof(float)), sample);
        }
        return bytesNeeded;
    }

    /// <summary>Compatibility wrapper for offline probes; real callbacks use DecodeInto.</summary>
    public byte[] Decode(byte[] source, int sourceBytes)
    {
        if (source is null || sourceBytes < 0 || sourceBytes > source.Length ||
            sourceBytes % _input.BlockAlign != 0 || sourceBytes > 4 * 1024 * 1024)
            throw new ArgumentException("Invalid source PCM packet size.", nameof(sourceBytes));
        var decoded = new byte[checked(sourceBytes / _input.BlockAlign *
            _channels * sizeof(float))];
        DecodeInto(source, sourceBytes, decoded);
        return decoded;
    }

    /// <summary>
    /// Conservative upper bound for output scratch across maximum ±250 ppm
    /// clock corrections and packet-edge history. Used ONCE when a route
    /// starts, outside the capture callback. Frames above this fail closed.
    /// </summary>
    public int RequiredOutputCapacity(int inputFrames)
    {
        // Offline route-tone probes legitimately encode a full 2-second
        // packet. The 500-ms limit belongs ONLY to the live capture callback,
        // not this reusable capacity helper or the existing 4-MB Encode API.
        if (inputFrames < 0 ||
            inputFrames > (4 * 1024 * 1024) / (_channels * sizeof(float)))
            throw new ArgumentOutOfRangeException(nameof(inputFrames));
        int frames = checked((int)Math.Ceiling(
            (inputFrames + 4.0) * _output.SampleRate /
            _input.SampleRate * (1.0 + AdaptiveBufferClock.MaximumCorrectionPpm * 1e-6)) + 16);
        return checked(frames * _output.BlockAlign);
    }

    /// <summary>
    /// Encode DSP output for the chosen physical mixer, retaining fractional
    /// sample position and the last source frame across callback boundaries.
    /// An unpaired final frame is deliberately not extrapolated during a rate
    /// change; no click-prone artificial padding is inserted. Downsampling
    /// applies a streaming anti-alias FIR in-place to the caller-owned PCM
    /// buffer before interpolation; input buffers must not be reused afterward.
    /// </summary>
    public int EncodeInto(byte[] floatPcm, int byteCount, Span<byte> output)
    {
        int floatFrameBytes = _channels * sizeof(float);
        if (byteCount < 0 || byteCount > floatPcm.Length ||
            byteCount % floatFrameBytes != 0 || byteCount > 4 * 1024 * 1024)
            throw new ArgumentException("Invalid native DSP packet size.", nameof(byteCount));
        int frames = byteCount / floatFrameBytes;
        if (frames == 0) return 0;
        // A bad scratch reservation cannot mutate FIR history or emit a
        // partially written render packet. The callback aborts this route.
        if (output.Length < RequiredOutputCapacity(frames))
            throw new ArgumentException(
                "Preallocated render PCM scratch too small.", nameof(output));
        _antiAlias?.ProcessInPlace(floatPcm.AsSpan(0, byteCount));

        // Capture side is the sole owner of Encode; the control loop
        // updates only the atomic correction once per 250 ms.
        double packetStep = _inputFramesPerOutputFrame *
            (1.0 + Volatile.Read(ref _rateAdjustmentPpm) * 0.000001);
        // The caller reserves a bound before entering WASAPI's callback.
        // Never allocate, resize, or wait for a larger output buffer here.
        if (output.Length % _output.BlockAlign != 0)
            throw new ArgumentException("Output scratch must hold complete PCM frames.", nameof(output));
        int capacityFrames = output.Length / _output.BlockAlign;
        int emitted = 0;
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
            if (emitted >= capacityFrames)
                throw new InvalidDataException("Preallocated output scratch overflow; stop unsafe route.");
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
                    channel * (_output.BitsPerSample / 8);
                if (_outputFloat)
                {
                    BinaryPrimitives.WriteSingleLittleEndian(
                        output.Slice(byteOffset), value);
                }
                else if (_output.BitsPerSample == 16)
                {
                    float scaled = value < 0f ? value * 32768f : value * 32767f;
                    BinaryPrimitives.WriteInt16LittleEndian(
                        output.Slice(byteOffset), (short)MathF.Round(scaled));
                }
                else
                {
                    float scaled = value < 0f ? value * 8388608f : value * 8388607f;
                    int signed24 = (int)MathF.Round(scaled);
                    if (_output.BitsPerSample == 24)
                    {
                        output[byteOffset] = (byte)signed24;
                        output[byteOffset + 1] = (byte)(signed24 >> 8);
                        output[byteOffset + 2] = (byte)(signed24 >> 16);
                    }
                    else
                    {
                        // Signed PCM24 occupies top 24 bits; bottom eight
                        // must remain zero, never reinterpret as IEEE float.
                        BinaryPrimitives.WriteInt32LittleEndian(
                            output.Slice(byteOffset), signed24 << 8);
                    }
                }
            }
            emitted++;
            _nextPosition += packetStep;
        }

        for (int c = 0; c < _channels; c++)
            _lastFrame[c] = ReadFloat(floatPcm, frames - 1, c);
        _hasLastFrame = true;
        _nextPosition -= frames;

        return checked(emitted * _output.BlockAlign);
    }

    /// <summary>Legacy offline test wrapper; WASAPI callback uses EncodeInto.</summary>
    public byte[] Encode(byte[] floatPcm, int byteCount)
    {
        if (floatPcm is null || byteCount < 0 || byteCount > floatPcm.Length ||
            byteCount % (_channels * sizeof(float)) != 0 || byteCount > 4 * 1024 * 1024)
            throw new ArgumentException("Invalid native DSP packet size.", nameof(byteCount));
        if (byteCount == 0) return Array.Empty<byte>();
        int frames = byteCount / (_channels * sizeof(float));
        byte[] output = new byte[RequiredOutputCapacity(frames)];
        int written = EncodeInto(floatPcm, byteCount, output);
        if (written != output.Length)
            Array.Resize(ref output, written);
        return output;
    }

    private float ReadFloat(ReadOnlySpan<byte> pcm, int frame, int channel) =>
        BinaryPrimitives.ReadSingleLittleEndian(
            pcm.Slice((frame * _channels + channel) * sizeof(float)));

    // Microsoft speaker-mask order is ascending speaker bit, not
    // necessarily intuitive "channel 0..N" order. Preserve front left/right,
    // fold center and each surround at -3 dB, and leave LFE out of stereo.
    // Normalize each row by its absolute sum to protect correlated full-scale
    // inputs from clipping; DSP processing still happens on every input channel
    // before this render-only mapping. No callback heap allocation.
    private static (float[] Left, float[] Right) BuildSurroundStereoMatrix(
        int speakerMask, int sourceChannels)
    {
        const float surround = 0.70710678f;
        float[] left = new float[sourceChannels];
        float[] right = new float[sourceChannels];
        uint remaining = unchecked((uint)speakerMask);
        for (int channel = 0; channel < sourceChannels; channel++)
        {
            uint speaker = remaining & (0u - remaining);
            remaining &= remaining - 1;
            switch (speaker)
            {
                case 0x1u: left[channel] = 1f; break;    // FL
                case 0x2u: right[channel] = 1f; break;   // FR
                case 0x4u: left[channel] = right[channel] = surround; break; // FC
                case 0x8u: break;                        // LFE is not full range
                case 0x10u: left[channel] = surround; break; // BL
                case 0x20u: right[channel] = surround; break; // BR
                case 0x200u: left[channel] = surround; break; // SL
                case 0x400u: right[channel] = surround; break; // SR
                default: throw new NotSupportedException(
                    "Unexpected speaker position in stereo downmix.");
            }
        }
        float lsum = left.Sum();
        float rsum = right.Sum();
        if (lsum < 1 || rsum < 1)
            throw new NotSupportedException("Surround mix is missing a front stereo pair.");
        for (int c = 0; c < sourceChannels; c++)
        {
            left[c] /= lsum;
            right[c] /= rsum;
        }
        return (left, right);
    }

    private float MixSurround(ReadOnlySpan<byte> samples, int frame, int sinkChannel)
    {
        float[] weights = sinkChannel == 0 ? _surroundLeft! : _surroundRight!;
        float total = 0f;
        for (int channel = 0; channel < _channels; channel++)
            total += ReadFloat(samples, frame, channel) * weights[channel];
        return total;
    }

    private float MixSurroundHistory(int sinkChannel)
    {
        float[] weights = sinkChannel == 0 ? _surroundLeft! : _surroundRight!;
        float total = 0f;
        for (int channel = 0; channel < _channels; channel++)
            total += _lastFrame[channel] * weights[channel];
        return total;
    }

    private float ReadMappedHistory(int sinkChannel)
    {
        if (_surroundLeft is not null)
            return MixSurroundHistory(sinkChannel);
        // Equal-weight stereo → mono preserves symmetric signals and
        // avoids summing two full-scale samples above the valid range.
        if (_channels == 2 && _outputChannels == 1)
            return (_lastFrame[0] + _lastFrame[1]) * 0.5f;
        // Mono → stereo duplicates the signal identically in both ears.
        return _lastFrame[_channels == 1 ? 0 : sinkChannel];
    }

    private float ReadMappedFloat(ReadOnlySpan<byte> pcm, int frame, int sinkChannel)
    {
        if (_surroundLeft is not null)
            return MixSurround(pcm, frame, sinkChannel);
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

        // Regression: 24-bit sign extension and asymmetric full-scale rails.
        var pcm24 = new WaveFormat(48000, 24, 1);
        var p24 = new PcmFormatAdapter(pcm24, ieee);
        byte[] packed = [0, 0, 0x80, 0, 0, 0, 0, 0, 0x40, 0xff, 0xff, 0x7f];
        byte[] unpacked = p24.Decode(packed, packed.Length);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(unpacked.AsSpan(0)), -1f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(unpacked.AsSpan(4)), 0f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(unpacked.AsSpan(8)), 0.5f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(unpacked.AsSpan(12)), 8388607f / 8388608f);
        byte[] encoded24 = new PcmFormatAdapter(ieee, pcm24).Encode(unpacked, unpacked.Length);
        // Positive PCM is normalized by 2^23 and quantized by 2^23-1,
        // so the maximum positive rail can legitimately differ by one LSB.
        // The sign, middle 24-bit sample and negative rail must survive.
        if (!encoded24.AsSpan(0, 9).SequenceEqual(packed.AsSpan(0, 9)) ||
            encoded24[10] != 0xff || encoded24[11] != 0x7f ||
            encoded24[9] is not (0xfe or 0xff))
            throw new InvalidOperationException("PCM24 signed full-scale round-trip corrupted.");

        if (IsSupported(new WaveFormat(48000, 32, 2)))
            throw new InvalidOperationException("Unimplemented integer PCM32 accepted as IEEE float32.");

        TestExtensiblePrecisionAndChannelLayouts();
        TestPcm24In32PrecisionAndFrames();
        TestStreamingResample(48000, 44100, 480, 2);
        TestStreamingResample(44100, 48000, 441, 2);
        TestStreamingResample(48000, 96000, 480, 2);
        TestStreamingResample(96000, 48000, 960, 2);

        TestMonoStereoMapping();
        TestSurroundToStereoDownmix();
        TestShortPacketBoundaries(44100, 48000);
        TestShortPacketBoundaries(48000, 44100);
        TestShortPacketBoundaries(48000, 96000);
        TestDownsampleAntiAlias(96000, 48000, 30000);
        TestDownsampleAntiAlias(48000, 44100, 23500);
        TestDownsampleChannelIsolation();
        TestAdjustableRateContinuity();
        TestSameRateFastPath();
        RunScratchSelfTests();

        try
        {
            _ = new PcmFormatAdapter(
                WaveFormat.CreateIeeeFloatWaveFormat(48000, 6),
                WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            throw new InvalidOperationException("Unsupported 5.1 to stereo mapping was accepted.");
        }
        catch (NotSupportedException) { }

        Console.WriteLine(
            "PASS: PCM16/PCM24/float32 conversion, saturation, 44.1/48/96 kHz " +
            "streaming, short-packet interpolation, mono/stereo and explicit 5.1/7.1 downmix, " +
            "windowed-sinc anti-alias rejection at 96->48 and 48->44.1 kHz, " +
            "channel isolation, same-rate allocation fast path, bounded clock " +
            "adjustment and unsupported maskless/ambiguous multichannel rejection.");
    }

    private static void TestExtensiblePrecisionAndChannelLayouts()
    {
        var packedStereo = new WaveFormatExtensible(48000, 24, 2);
        var floatStereo = new WaveFormatExtensible(48000, 32, 2);
        var aligned24 = new WaveFormatExtensible(48000, 32, 2, false, 24, 0x3);
        if (!IsSupported(packedStereo) || !IsSupported(floatStereo) ||
            !IsSupported(aligned24))
            throw new InvalidOperationException("Full-width extensible PCM24/float32 was rejected.");
        foreach (var bad in new WaveFormat[]
        {
            new WaveFormatExtensible(48000, 24, 2, false, 20, 0x3),
            new WaveFormatExtensible(48000, 32, 2, true, 24, 0x3),
            new WaveFormatExtensible(48000, 24, 2, false, 24, 0x1),
            new WaveFormatExtensible(48000, 24, 6, false, 24, 0),
            new WaveFormat(48000, 24, 6)
        })
        {
            if (IsSupported(bad))
                throw new InvalidOperationException("Ambiguous valid bits or channel mask was accepted.");
        }
        var surroundPcm = new WaveFormatExtensible(48000, 24, 6, false, 24, 0x3f);
        var surroundFloat = new WaveFormatExtensible(48000, 32, 6, true, 32, 0x3f);
        _ = new PcmFormatAdapter(surroundPcm, surroundFloat);
        var wrongSpeakers = new WaveFormatExtensible(48000, 32, 6, true, 32, 0x60f);
        if (!IsSupported(wrongSpeakers))
            throw new InvalidOperationException("Valid alternate 5.1 layout unexpectedly rejected.");
        try
        {
            _ = new PcmFormatAdapter(surroundPcm, wrongSpeakers);
            throw new InvalidOperationException("Mismatched 5.1 speaker layouts were accepted.");
        }
        catch (NotSupportedException) { }
        _ = new PcmFormatAdapter(new WaveFormatExtensible(48000, 24, 8),
            new WaveFormatExtensible(48000, 32, 8));
    }

    private static void TestPcm24In32PrecisionAndFrames()
    {
        var alignedFormat = new WaveFormatExtensible(48000, 32, 1, false, 24, 0x4);
        var ieee = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
        byte[] aligned = [
            0x00, 0x00, 0x00, 0x80,  // -8388608 / 8388608
            0x00, 0x00, 0x00, 0x00,  // zero
            0x00, 0x00, 0x00, 0x40,  // +0.5
            0x00, 0xff, 0xff, 0x7f   // +8388607 / 8388608
        ];
        var decoder = new PcmFormatAdapter(alignedFormat, ieee);
        byte[] floats = decoder.Decode(aligned, aligned.Length);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(floats.AsSpan(0)), -1f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(floats.AsSpan(4)), 0f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(floats.AsSpan(8)), 0.5f);
        AssertClose(BinaryPrimitives.ReadSingleLittleEndian(floats.AsSpan(12)),
            8388607f / 8388608f);
        byte[] roundtrip = new PcmFormatAdapter(ieee, alignedFormat)
            .Encode(floats, floats.Length);
        if (roundtrip.Length != aligned.Length ||
            !roundtrip.AsSpan(0, 12).SequenceEqual(aligned.AsSpan(0, 12)) ||
            roundtrip[12] != 0 || roundtrip[15] != 0x7f ||
            roundtrip[14] != 0xff || roundtrip[13] is not (0xfe or 0xff))
            throw new InvalidOperationException("PCM24-in-32 rail/zero padding corrupted.");

        byte[] invalid = (byte[])aligned.Clone();
        invalid[8] = 1; // lowest eight bits may not contain signal.
        try
        {
            decoder.DecodeInto(invalid, invalid.Length, new byte[16]);
            throw new InvalidOperationException("Nonzero PCM24-in-32 pad accepted.");
        }
        catch (InvalidDataException) { }
        if (IsSupported(new WaveFormat(48000, 32, 1)) ||
            IsSupported(new WaveFormatExtensible(48000, 32, 1, false, 32, 0x4)) ||
            IsSupported(new WaveFormatExtensible(48000, 32, 1, true, 24, 0x4)))
            throw new InvalidOperationException("PCM32/partial-float alias accepted.");

        // Compare two successive stereo formats through actual adapter
        // decode->encode paths at complete 24-bit sample boundaries.
        var packedFormat = new WaveFormat(44100, 24, 2);
        var alignedStereo = new WaveFormatExtensible(44100, 32, 2, false, 24, 0x3);
        var packedToAligned = new PcmFormatAdapter(packedFormat, alignedStereo);
        var alignedToPacked = new PcmFormatAdapter(alignedStereo, packedFormat);
        byte[] packed = new byte[18]; // 3 stereo frames
        for (int frame = 0; frame < 3; frame++)
        {
            int offset = frame * 6;
            packed[offset + 2] = 0x40; // left +0.5
            packed[offset + 5] = 0xc0; // right -0.5
        }
        byte[] normalized = packedToAligned.Decode(packed, packed.Length);
        byte[] padded = packedToAligned.Encode(normalized, normalized.Length);
        byte[] normalizedAgain = alignedToPacked.Decode(padded, padded.Length);
        byte[] unpacked = alignedToPacked.Encode(normalizedAgain, normalizedAgain.Length);
        if (unpacked.Length != packed.Length ||
            !unpacked.AsSpan().SequenceEqual(packed))
            throw new InvalidOperationException(
                "PCM24 pack/unpack changed signs, channels, or sample alignment.");
        Console.WriteLine("PASS: PCM24-in-32 signed left alignment, pad zeros, frame conversion and no PCM32/float aliases.");
    }

    // Same real streaming implementation: old allocating convenience calls
    // versus new bounded callback buffers, including frame history, rate
    // correction, mono/stereo and eight-channel channel alignment.
    public static void RunScratchSelfTests()
    {
        foreach ((int inputRate, int outputRate, int sourceChannels,
                  int sinkChannels, int sourceBits) in new[]
        {
            (44100, 48000, 2, 2, 16),
            (44100, 48000, 2, 2, 24),
            (48000, 44100, 2, 2, 32),
            (96000, 48000, 2, 2, 24),
            (96000, 48000, 2, 2, 32),
            (48000, 96000, 1, 2, 32),
            (48000, 48000, 2, 1, 16),
            (48000, 48000, 8, 8, 24),
            (48000, 48000, 8, 8, 32),
            (48000, 48000, 6, 2, 24),
            (96000, 48000, 8, 2, 32)
        })
        {
            var input = sourceChannels > 2
                ? new WaveFormatExtensible(inputRate, sourceBits, sourceChannels,
                    sourceBits == 32, sourceBits,
                    sourceChannels == 6 ? 0x3f : 0x63f)
                : sourceBits == 32
                    ? WaveFormat.CreateIeeeFloatWaveFormat(inputRate, sourceChannels)
                    : new WaveFormat(inputRate, sourceBits, sourceChannels);
            var output = sinkChannels > 2
                ? new WaveFormatExtensible(outputRate, 32, sinkChannels,
                    true, 32, sinkChannels == 6 ? 0x3f : 0x63f)
                : WaveFormat.CreateIeeeFloatWaveFormat(outputRate, sinkChannels);
            var allocating = new PcmFormatAdapter(input, output);
            var scratch = new PcmFormatAdapter(input, output);
            var monoPacket = new byte[checked(1024 * input.BlockAlign)];
            var decoded = new byte[checked(1024 * sourceChannels * sizeof(float))];
            var rendered = new byte[scratch.RequiredOutputCapacity(1024)];
            int[] fragments = [1, 3, 511, 15, 7, 487];
            long comparedBytes = 0;
            int absoluteFrame = 0;
            for (int packet = 0; packet < 60; packet++)
            {
                int frames = fragments[packet % fragments.Length];
                for (int frame = 0; frame < frames; frame++)
                {
                    for (int ch = 0; ch < sourceChannels; ch++)
                    {
                        float sample = 0.125f * MathF.Sin(2f * MathF.PI * 1000f *
                            (absoluteFrame + frame) / inputRate);
                        if (ch % 2 != 0) sample = -sample;
                        int offset = frame * input.BlockAlign +
                            ch * (sourceBits / 8);
                        if (sourceBits == 16)
                            BinaryPrimitives.WriteInt16LittleEndian(
                                monoPacket.AsSpan(offset),
                                (short)MathF.Round(sample * 32767));
                        else if (sourceBits == 24)
                        {
                            int signed24 = (int)MathF.Round(sample * 8388607f);
                            monoPacket[offset] = (byte)signed24;
                            monoPacket[offset + 1] = (byte)(signed24 >> 8);
                            monoPacket[offset + 2] = (byte)(signed24 >> 16);
                        }
                        else
                            BinaryPrimitives.WriteSingleLittleEndian(
                                monoPacket.AsSpan(offset), sample);
                    }
                }
                absoluteFrame += frames;
                int inputLength = frames * input.BlockAlign;
                byte[] oldDecoded = allocating.Decode(monoPacket, inputLength);
                int decodedLength = scratch.DecodeInto(
                    monoPacket, inputLength, decoded);
                // Encoding (downsample FIR) MUTATES decoded PCM in-place, so
                // compare both decoders BEFORE asking either to resample.
                if (decodedLength != oldDecoded.Length ||
                    !decoded.AsSpan(0, decodedLength).SequenceEqual(oldDecoded))
                    throw new InvalidOperationException("Scratch decoding differs from original float32 PCM.");
                byte[] oldEncoded = allocating.Encode(oldDecoded, oldDecoded.Length);
                int actual = scratch.EncodeInto(decoded, decodedLength, rendered);
                if (!rendered.AsSpan(0, actual).SequenceEqual(oldEncoded))
                    throw new InvalidOperationException(
                        "Reused scratch differs from original streaming conversion at packet boundaries.");
                comparedBytes += actual;
            }
            if (comparedBytes < 1000)
                throw new InvalidOperationException("No meaningful native bridge PCM was compared.");

            // Fail closed BEFORE altering buffers or any FIR/stream state.
            try
            {
                scratch.DecodeInto(monoPacket, 5 * input.BlockAlign,
                    new byte[sizeof(float)]);
                throw new InvalidOperationException("Undersized decode scratch was accepted.");
            }
            catch (ArgumentException) { }
            try
            {
                scratch.EncodeInto(decoded, 5 * sourceChannels * sizeof(float),
                    new byte[output.BlockAlign]);
                throw new InvalidOperationException("Undersized render scratch was accepted.");
            }
            catch (ArgumentException) { }
        }

        // Validate that the actual repeated float32 converter path is
        // allocation-free once the scratch and JIT are warmed up. This is
        // an in-memory managed heap measurement, NOT a physical WASAPI
        // callback deadline or an end-to-end low latency benchmark.
        var fmt = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var reuse = new PcmFormatAdapter(fmt, fmt);
        var packetBytes = new byte[480 * fmt.BlockAlign];
        var decodeScratch = new byte[packetBytes.Length];
        var outScratch = new byte[reuse.RequiredOutputCapacity(480)];
        for (int i = 0; i < 96; i++)
        {
            int size = reuse.DecodeInto(packetBytes, packetBytes.Length, decodeScratch);
            reuse.EncodeInto(decodeScratch, size, outScratch);
        }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 256; i++)
        {
            int size = reuse.DecodeInto(packetBytes, packetBytes.Length, decodeScratch);
            reuse.EncodeInto(decodeScratch, size, outScratch);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated > 256)
            throw new InvalidOperationException(
                "Audio callback PCM scratch path allocated managed heap memory: " +
                allocated + " bytes over 256 warm packets.");
        Console.WriteLine(
            "PASS: 11 PCM16/PCM24/float32 44.1/48/96k mono/stereo/5.1/7.1 equivalence " +
            "matrices x60 fragmented packets, scratch overflow guards and " +
            "256 warm conversion packets without managed PCM allocations.");
    }

    private static void TestSurroundToStereoDownmix()
    {
        var stereo = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        const float centerWeight = 0.70710678f;
        foreach ((int mask, int channels) in new[] {
            (0x3f, 6), (0x60f, 6), (0x63f, 8)
        })
        {
            var source = new WaveFormatExtensible(
                48000, 32, channels, true, 32, mask);
            var adapter = new PcmFormatAdapter(source, stereo);
            int frames = 16;
            byte[] input = new byte[frames * channels * sizeof(float)];
            // Every speaker receives one isolated frame. No channel swapping,
            // and no LFE leaked into the left/right full-range speakers.
            int[] bits = Enumerable.Range(0, 32)
                .Select(i => 1 << i).Where(bit => (mask & bit) != 0).ToArray();
            float denominator = 1f + centerWeight +
                (mask == 0x63f ? 2f * centerWeight : centerWeight);
            for (int channel = 0; channel < channels; channel++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(
                    input.AsSpan((channel * channels + channel) * sizeof(float)), 1f);
            }
            byte[] result = adapter.Encode(input, input.Length);
            if (result.Length != frames * stereo.BlockAlign)
                throw new InvalidOperationException("Surround downmix changed frame count.");
            for (int channel = 0; channel < channels; channel++)
            {
                int speaker = bits[channel];
                float left = BinaryPrimitives.ReadSingleLittleEndian(
                    result.AsSpan(channel * stereo.BlockAlign));
                float right = BinaryPrimitives.ReadSingleLittleEndian(
                    result.AsSpan(channel * stereo.BlockAlign + 4));
                float expectedL = speaker switch
                {
                    0x1 => 1f, 0x4 or 0x10 or 0x200 => centerWeight, _ => 0f
                };
                float expectedR = speaker switch
                {
                    0x2 => 1f, 0x4 or 0x20 or 0x400 => centerWeight, _ => 0f
                };
                AssertClose(left, expectedL / denominator);
                AssertClose(right, expectedR / denominator);
            }

            // All full-scale channels correlated: normalized stereo may not
            // exceed unity, regardless of the number of surround pairs.
            var allOnes = new byte[frames * channels * sizeof(float)];
            for (int sample = 0; sample < frames * channels; sample++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    allOnes.AsSpan(sample * 4), 1f);
            var positive = new PcmFormatAdapter(source, stereo)
                .Encode(allOnes, allOnes.Length);
            for (int i = 0; i < positive.Length; i += 4)
                AssertClose(BinaryPrimitives.ReadSingleLittleEndian(
                    positive.AsSpan(i)), 1f);

            // Nonstandard layouts and non-front stereo sinks must never map.
            foreach (int invalidMask in new[] { 0x33f, 0x55f })
            {
                if (BitOperations.PopCount(unchecked((uint)invalidMask)) != channels)
                    continue;
                try
                {
                    _ = new PcmFormatAdapter(
                        new WaveFormatExtensible(48000, 32, channels, true,
                            32, invalidMask), stereo);
                    throw new InvalidOperationException("Unknown surround layout accepted.");
                }
                catch (NotSupportedException) { }
            }
        }
        try
        {
            _ = new PcmFormatAdapter(
                new WaveFormatExtensible(48000, 32, 6, true, 32, 0x3f),
                new WaveFormatExtensible(48000, 32, 2, true, 32, 0xC));
            throw new InvalidOperationException("Non-front two-channel sink accepted.");
        }
        catch (NotSupportedException) { }
        Console.WriteLine("PASS: speaker-aware 5.1 back/side and 7.1 stereo downmix, LFE exclusion, " +
            "bounded coherent peaks, channel isolation and invalid-layout rejection.");
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

    private static void TestSameRateFastPath()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var adapter = new PcmFormatAdapter(format, format);
        for (int size = 1; size <= 1000; size += 17)
        {
            byte[] input = new byte[size * format.BlockAlign];
            for (int i = 0; i < size * 2; i++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    input.AsSpan(i * sizeof(float)), 0.0625f);
            var output = adapter.Encode(input, input.Length);
            if (output.Length != input.Length || !input.AsSpan().SequenceEqual(output))
                throw new InvalidOperationException(
                    "Same-rate fast path changed PCM content or frame count.");
        }
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
