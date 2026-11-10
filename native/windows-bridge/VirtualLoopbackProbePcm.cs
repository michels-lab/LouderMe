using System.Buffers.Binary;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// LAB-only virtual WASAPI listener. PCM16, packed PCM24 and IEEE float32 are decoded to
/// private float32 scratch for the SAME 440Hz loopback identity verifier.
/// This does not open an audio device, manufacture PCM in external mode,
/// initialize DSP, or select any physical sink.
/// </summary>
internal sealed class VirtualLoopbackProbePcm
{
    private readonly PcmFormatAdapter _decoder;
    private readonly byte[] _floatScratch;

    public WaveFormat EvidenceFormat { get; }

    public VirtualLoopbackProbePcm(WaveFormat captureFormat)
    {
        ArgumentNullException.ThrowIfNull(captureFormat);
        if (!PcmFormatAdapter.IsSupported(captureFormat) ||
            captureFormat.Channels is < 1 or > 8)
            throw new NotSupportedException(
                "Virtual loopback probe needs supported PCM16, PCM24 or IEEE float32, mono-to-eight-channel mix.");
        // A valid channel count alone cannot identify the speakers.
        // Fail closed on any multichannel format that isn't one of the
        // actual SysVAD 5.1 back/side or 7.1 Windows render masks.
        if (captureFormat.Channels > 2 &&
            (captureFormat is not WaveFormatExtensible ext ||
             (captureFormat.Channels == 6 &&
              ext.ChannelMask is not (0x3f or 0x60f)) ||
             (captureFormat.Channels == 8 && ext.ChannelMask != 0x63f) ||
             captureFormat.Channels is not (6 or 8)))
            throw new NotSupportedException(
                "Virtual surround PCM requires explicit standard 5.1/7.1 speaker positions.");
        EvidenceFormat = captureFormat.Channels > 2
            ? new WaveFormatExtensible(captureFormat.SampleRate, 32,
                captureFormat.Channels, ((WaveFormatExtensible)captureFormat).ChannelMask)
            : WaveFormat.CreateIeeeFloatWaveFormat(
                captureFormat.SampleRate, captureFormat.Channels);
        _decoder = new PcmFormatAdapter(captureFormat, EvidenceFormat);
        // Reserve once, BEFORE the WASAPI callback. Overlarge capture packets
        // reject safely rather than allocate under an audio callback.
        int maxFrames = checked(captureFormat.SampleRate / 2 + 16);
        _floatScratch = new byte[checked(maxFrames *
            captureFormat.Channels * sizeof(float))];
    }

    public void AddPacket(LoopbackToneVerifier verifier, byte[] pcm, int recordedBytes)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        if (recordedBytes == 0) return;
        int decodedLength = _decoder.DecodeInto(pcm, recordedBytes, _floatScratch);
        verifier.Add(_floatScratch.AsSpan(0, decodedLength));
    }

    // Alternative receive-only verifier: observe exact speaker positions
    // without copying float32 PCM or allocating inside WASAPI callbacks.
    public void AddPacket(VirtualSpeakerMapVerifier verifier, byte[] pcm, int recordedBytes)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        if (recordedBytes == 0) return;
        int length = _decoder.DecodeInto(pcm, recordedBytes, _floatScratch);
        verifier.Add(_floatScratch.AsSpan(0, length));
    }

    public static byte[] CreateInternalTone(WaveFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (!PcmFormatAdapter.IsSupported(format) ||
            format.Channels is < 1 or > 8)
            throw new NotSupportedException("Owned tone requires PCM16, PCM24 or IEEE float32 mix.");
        // Only called by --probe-virtual (own stimulus); never called by
        // --probe-virtual-external (a separate Windows executable is source).
        int frames = checked(format.SampleRate * 2);
        var pcm = new byte[checked(frames * format.BlockAlign)];
        bool isFloat = PcmFormatAdapter.IsFloat(format);
        for (int frame = 0; frame < frames; frame++)
        {
            float value = 0.025f * MathF.Sin(
                2 * MathF.PI * 440f * frame / format.SampleRate);
            for (int channel = 0; channel < format.Channels; channel++)
            {
                int at = frame * format.BlockAlign +
                    channel * (format.BitsPerSample / 8);
                if (isFloat)
                    BinaryPrimitives.WriteSingleLittleEndian(pcm.AsSpan(at), value);
                else if (format.BitsPerSample == 16)
                    BinaryPrimitives.WriteInt16LittleEndian(
                        pcm.AsSpan(at), (short)MathF.Round(value * 32767f));
                else
                {
                    int signed24 = (int)MathF.Round(value * 8388607f);
                    if (format.BitsPerSample == 24)
                    {
                        pcm[at] = (byte)signed24;
                        pcm[at + 1] = (byte)(signed24 >> 8);
                        pcm[at + 2] = (byte)(signed24 >> 16);
                    }
                    else
                        BinaryPrimitives.WriteInt32LittleEndian(
                            pcm.AsSpan(at), signed24 << 8);
                }
            }
        }
        return pcm;
    }

    public static void RunSelfTests()
    {
        foreach (int rate in new[] { 44100, 48000, 96000 })
        {
            foreach (int sampleBits in new[] { 16, 24, 32, 132 })
            {
                foreach ((int channels, int mask) in new[] {
                    (1, 0x4), (2, 0x3), (6, 0x3f), (6, 0x60f), (8, 0x63f)
                })
                {
                    WaveFormat capture = channels > 2 || sampleBits == 132
                        ? new WaveFormatExtensible(rate, sampleBits == 132 ? 32 : sampleBits,
                            channels, sampleBits == 32, sampleBits == 132 ? 24 : sampleBits, mask)
                        : sampleBits == 32
                            ? WaveFormat.CreateIeeeFloatWaveFormat(rate, channels)
                            : new WaveFormat(rate, sampleBits, channels);
                    var decoder = new VirtualLoopbackProbePcm(capture);
                    var verifier = new LoopbackToneVerifier(
                        decoder.EvidenceFormat, requireEverySpeakerLane: true);
                    byte[] tone = CreateInternalTone(capture);
                    int framesPerPacket = 137;
                    int cursor = 0;
                    while (cursor < tone.Length)
                    {
                        int bytes = Math.Min(tone.Length - cursor,
                            framesPerPacket * capture.BlockAlign);
                        byte[] segment = new byte[bytes];
                        Buffer.BlockCopy(tone, cursor, segment, 0, bytes);
                        decoder.AddPacket(verifier, segment, bytes);
                        cursor += bytes;
                        framesPerPacket = framesPerPacket == 137 ? 521 : 137;
                    }
                    var e = verifier.Evaluate();
                    if (!e.Passed || e.Matched440Amplitude is < 0.02 or > 0.03 ||
                        e.Coherence < 0.96)
                        throw new InvalidOperationException(
                            $"Virtual {rate}Hz {channels}ch {(sampleBits == 132 ? "PCM24-in-32" : sampleBits == 32 ? "float32" : $"PCM{sampleBits}")} proof failed: {e}");
                    // No source must not be accepted, regardless of encoding.
                    var quiet = new LoopbackToneVerifier(
                        decoder.EvidenceFormat, requireEverySpeakerLane: true);
                    byte[] silence = new byte[rate / 4 * capture.BlockAlign];
                    decoder.AddPacket(quiet, silence, silence.Length);
                    if (quiet.Evaluate().Passed)
                        throw new InvalidOperationException("No-source virtual capture accepted silence.");
                }
            }
        }

        foreach (WaveFormat invalid in new WaveFormat[] {
            new WaveFormat(48000, 24, 6),
            new WaveFormatExtensible(48000, 24, 6, false, 24, 0),
            new WaveFormatExtensible(48000, 24, 6, false, 24, 0x33f),
            new WaveFormatExtensible(48000, 32, 8, true, 32, 0x3f)
        })
        {
            try
            {
                _ = new VirtualLoopbackProbePcm(invalid);
                throw new InvalidOperationException(
                    "Unsupported virtual surround mask was accepted.");
            }
            catch (NotSupportedException) { }
        }

        var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var floatProbe = new VirtualLoopbackProbePcm(floatFormat);
        var verifierFloat = new LoopbackToneVerifier(floatProbe.EvidenceFormat);
        var nan = new byte[floatFormat.BlockAlign];
        BinaryPrimitives.WriteSingleLittleEndian(nan, float.NaN);
        try
        {
            floatProbe.AddPacket(verifierFloat, nan, nan.Length);
            throw new InvalidOperationException("Nonfinite float32 virtual sample accepted.");
        }
        catch (InvalidDataException) { }

        var pcm16Format = new WaveFormat(48000, 16, 2);
        var pcmProbe = new VirtualLoopbackProbePcm(pcm16Format);
        try
        {
            pcmProbe.AddPacket(new LoopbackToneVerifier(pcmProbe.EvidenceFormat),
                new byte[7], 7);
            throw new InvalidOperationException("Unaligned PCM16 virtual packet accepted.");
        }
        catch (ArgumentException) { }
        try
        {
            int overFrames = pcm16Format.SampleRate / 2 + 17;
            pcmProbe.AddPacket(new LoopbackToneVerifier(pcmProbe.EvidenceFormat),
                new byte[overFrames * pcm16Format.BlockAlign],
                overFrames * pcm16Format.BlockAlign);
            throw new InvalidOperationException("Oversized virtual PCM packet allocated or accepted.");
        }
        catch (ArgumentException) { }
        // A malformed six-byte PCM24 stereo frame must not be silently
        // truncated or counted as acoustic evidence.
        var pcm24Format = new WaveFormat(48000, 24, 2);
        var pcm24Probe = new VirtualLoopbackProbePcm(pcm24Format);
        try
        {
            pcm24Probe.AddPacket(new LoopbackToneVerifier(pcm24Probe.EvidenceFormat),
                new byte[5], 5);
            throw new InvalidOperationException("Unaligned packed PCM24 frame accepted.");
        }
        catch (ArgumentException) { }
        try
        {
            int overFrames = pcm24Format.SampleRate / 2 + 17;
            pcm24Probe.AddPacket(new LoopbackToneVerifier(pcm24Probe.EvidenceFormat),
                new byte[overFrames * pcm24Format.BlockAlign],
                overFrames * pcm24Format.BlockAlign);
            throw new InvalidOperationException("Oversized packed PCM24 virtual packet accepted.");
        }
        catch (ArgumentException) { }
        try
        {
            _ = new VirtualLoopbackProbePcm(new WaveFormat(48000, 32, 2));
            throw new InvalidOperationException("Unimplemented integer PCM32 accepted as IEEE float32.");
        }
        catch (NotSupportedException) { }

        Console.WriteLine("PASS: virtual WASAPI receive-only PCM16/PCM24/float32 decoder and owned-tone encode " +
            "44.1/48/96k mono/stereo/5.1 back+side/7.1, every-lane 440Hz identity, " +
            "reject silence, unknown speaker masks, unaligned/oversized PCM, NaN and integer PCM32.");
    }
}
