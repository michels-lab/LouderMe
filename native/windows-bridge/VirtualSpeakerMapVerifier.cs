using System.Buffers.Binary;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Receive-only virtual-WASAPI proof of speaker POSITION, not just the
/// presence of 440Hz in every lane. A separately launched Windows app emits
/// 440+53*lane Hz in each channel, with normal 5.1/7.1 WAVEFORMATEXTENSIBLE
/// ordering. This bridge never generates those signals or opens a sink.
/// </summary>
internal sealed class VirtualSpeakerMapVerifier
{
    private readonly int _rate;
    private readonly int _channels;
    private readonly double[] _sines;
    private readonly double[] _cosines;
    private long _frames;
    private double _peak;

    private static int Frequency(int lane) => 440 + 53 * lane;

    internal VirtualSpeakerMapVerifier(WaveFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (format.BitsPerSample != 32 ||
            (format.Encoding != WaveFormatEncoding.IeeeFloat &&
             (format is not WaveFormatExtensible ext ||
              ext.SubFormat != new Guid("00000003-0000-0010-8000-00aa00389b71"))) ||
            format.SampleRate is < 8000 or > 192000 ||
            format.Channels is not (1 or 2 or 6 or 8) ||
            format.BlockAlign != format.Channels * sizeof(float))
            throw new NotSupportedException(
                "Speaker-map evidence needs float32 1/2/6/8 channels with known Windows speaker ordering.");
        if (format.Channels > 2 &&
            (format is not WaveFormatExtensible surround ||
             (format.Channels == 6 && surround.ChannelMask is not (0x3f or 0x60f)) ||
             (format.Channels == 8 && surround.ChannelMask != 0x63f)))
            throw new NotSupportedException("Speaker-map evidence needs explicit 5.1 back/side or 7.1 masks.");
        _rate = format.SampleRate;
        _channels = format.Channels;
        // Fixed once before capture; no managed allocation per sample/callback.
        _sines = new double[_channels * _channels];
        _cosines = new double[_channels * _channels];
    }

    internal void Add(ReadOnlySpan<byte> samples)
    {
        if (samples.Length % (_channels * sizeof(float)) != 0)
            throw new InvalidDataException("Speaker-map PCM packet split a frame.");
        Span<double> sin = stackalloc double[8];
        Span<double> cos = stackalloc double[8];
        for (int offset = 0; offset < samples.Length; offset += _channels * sizeof(float))
        {
            double time = (_frames % _rate) * 2.0 * Math.PI / _rate;
            for (int frequency = 0; frequency < _channels; frequency++)
            {
                double radians = time * Frequency(frequency);
                sin[frequency] = Math.Sin(radians);
                cos[frequency] = Math.Cos(radians);
            }
            for (int channel = 0; channel < _channels; channel++)
            {
                double value = BinaryPrimitives.ReadSingleLittleEndian(
                    samples.Slice(offset + channel * sizeof(float), sizeof(float)));
                if (!double.IsFinite(value))
                    throw new InvalidDataException("Speaker-map WASAPI PCM included NaN/Infinity.");
                _peak = Math.Max(_peak, Math.Abs(value));
                for (int frequency = 0; frequency < _channels; frequency++)
                {
                    int at = channel * _channels + frequency;
                    _sines[at] += value * sin[frequency];
                    _cosines[at] += value * cos[frequency];
                }
            }
            _frames++;
        }
    }

    internal SpeakerMapEvidence Evaluate()
    {
        if (_frames < _rate / 2)
            return new SpeakerMapEvidence(_frames, _channels, 0, 0, false);

        double minOwn = double.MaxValue;
        double maxForeign = 0;
        bool distinct = true;
        for (int channel = 0; channel < _channels; channel++)
        {
            double own = 0, strongestAlien = 0;
            for (int frequency = 0; frequency < _channels; frequency++)
            {
                int at = channel * _channels + frequency;
                double projection = (2.0 / _frames) *
                    Math.Sqrt(_sines[at] * _sines[at] +
                              _cosines[at] * _cosines[at]);
                if (frequency == channel) own = projection;
                else strongestAlien = Math.Max(strongestAlien, projection);
            }
            minOwn = Math.Min(minOwn, own);
            maxForeign = Math.Max(maxForeign, strongestAlien);
            if (own < 0.004 || strongestAlien > own * 0.30)
                distinct = false;
        }
        return new SpeakerMapEvidence(_frames, _channels, minOwn, maxForeign,
            distinct && _peak is >= 0.01 and <= 0.15);
    }

    internal readonly record struct SpeakerMapEvidence(
        long Frames, int Channels, double MinOwnAmplitude,
        double MaxForeignAmplitude, bool Passed);

    internal static void RunSelfTests()
    {
        foreach (int rate in new[] { 8000, 48000 })
        foreach ((int channels, int mask) in new[] {
            (1, 0x4), (2, 0x3), (6, 0x3f), (6, 0x60f), (8, 0x63f)
        })
        {
            var format = channels == 1
                ? WaveFormat.CreateIeeeFloatWaveFormat(rate, 1)
                : new WaveFormatExtensible(rate, 32, channels, true, 32, mask);
            // Independently generated fixture with irregular callback boundaries.
            var good = Generate(format);
            if (!good.Passed || good.MinOwnAmplitude < 0.01 ||
                good.MaxForeignAmplitude > 0.003)
                throw new InvalidOperationException(
                    "Virtual speaker-map rejected correctly ordered lanes: " + good);
            if (rate == 8000)
            {
                for (int badLane = 0; badLane < channels; badLane++)
                {
                    if (Generate(format, mutedLane: badLane).Passed)
                        throw new InvalidOperationException(
                            "Virtual speaker-map accepted a missing channel " + badLane);
                    if (channels > 1 && Generate(format, swappedLane: badLane).Passed)
                        throw new InvalidOperationException(
                            "Virtual speaker-map accepted a switched speaker " + badLane);
                }
                if (Generate(format, allSameTone: true).Passed)
                    throw new InvalidOperationException(
                        "Virtual speaker-map accepted identical 440Hz in all speakers.");
            }
        }
        var wrong = WaveFormat.CreateIeeeFloatWaveFormat(48000, 3);
        try
        {
            _ = new VirtualSpeakerMapVerifier(wrong);
            throw new InvalidOperationException("Ambiguous 3-channel map accepted.");
        }
        catch (NotSupportedException) { }
        Console.WriteLine(
            "PASS: speaker identity 1/2/5.1 back/side/7.1 detects missing, swapped, " +
            "or identical-frequency lanes with irregular WASAPI packet boundaries.");
    }

    private static SpeakerMapEvidence Generate(WaveFormat format,
        int? mutedLane = null, int? swappedLane = null, bool allSameTone = false)
    {
        int channels = format.Channels;
        int rate = format.SampleRate;
        var verifier = new VirtualSpeakerMapVerifier(format);
        // 2s source plus 0.5s silence on either side: unknown WASAPI start.
        int totalFrames = rate * 3;
        int start = rate / 2;
        int cursor = 0, chunk = 0;
        while (cursor < totalFrames)
        {
            int frames = Math.Min(totalFrames - cursor,
                new[] { 137, 521, 47, 819 }[chunk++ % 4]);
            var data = new byte[frames * format.BlockAlign];
            for (int i = 0; i < frames; i++)
            {
                int absolute = cursor + i;
                if (absolute < start || absolute >= start + 2 * rate) continue;
                for (int lane = 0; lane < channels; lane++)
                {
                    int signalLane = swappedLane.HasValue && channels > 1
                        ? lane == swappedLane.Value ? (lane + 1) % channels
                          : lane == (swappedLane.Value + 1) % channels
                            ? swappedLane.Value : lane
                        : lane;
                    float sample = mutedLane == lane ? 0 :
                        0.025f * MathF.Sin(
                            2f * MathF.PI *
                            (allSameTone ? Frequency(0) : Frequency(signalLane)) *
                            absolute / rate);
                    BinaryPrimitives.WriteSingleLittleEndian(
                        data.AsSpan((i * channels + lane) * sizeof(float)), sample);
                }
            }
            verifier.Add(data);
            cursor += frames;
        }
        return verifier.Evaluate();
    }
}
