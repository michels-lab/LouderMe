using System.Buffers.Binary;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Device-free, streaming evidence that a 440 Hz signal returned by WASAPI
/// loopback is frequency-matched to the tone sent to the LAB virtual speaker.
/// This is not an audible physical-route proof, a driver certification or a
/// guarantee of third-party/global system-audio capture.
/// </summary>
internal sealed class LoopbackToneVerifier
{
    public const int FrequencyHz = 440;
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly double _radiansPerFrame;
    private long _frames;
    private double _energy;
    private double _projectionSin;
    private double _projectionCos;
    private double _peak;

    public LoopbackToneVerifier(WaveFormat mix)
    {
        ArgumentNullException.ThrowIfNull(mix);
        if (mix.BitsPerSample != 32 ||
            (mix.Encoding != WaveFormatEncoding.IeeeFloat &&
             (mix is not WaveFormatExtensible extensible ||
              extensible.SubFormat != new Guid("00000003-0000-0010-8000-00aa00389b71"))) ||
            mix.Channels is < 1 or > 8 ||
            mix.SampleRate is < 8000 or > 192000 ||
            mix.BlockAlign != mix.Channels * sizeof(float))
            throw new NotSupportedException(
                "Lab tone proof requires float32 mono-to-eight-channel WASAPI mix.");
        _sampleRate = mix.SampleRate;
        _channels = mix.Channels;
        _radiansPerFrame = 2 * Math.PI * FrequencyHz / mix.SampleRate;
    }

    public void Add(ReadOnlySpan<byte> pcm)
    {
        int blockSize = checked(_channels * sizeof(float));
        if (pcm.Length % blockSize != 0)
            throw new InvalidDataException("Misaligned float32 loopback packet.");
        for (int index = 0; index < pcm.Length; index += blockSize)
        {
            double mono = 0;
            for (int channel = 0; channel < _channels; channel++)
            {
                var value = BinaryPrimitives.ReadSingleLittleEndian(
                    pcm.Slice(index + channel * sizeof(float), sizeof(float)));
                if (!float.IsFinite(value))
                    throw new InvalidDataException("Nonfinite PCM from virtual loopback.");
                mono += value;
            }

            // Our lab stimulus is identical on all channels. Averaging allows
            // an unmodified mono/stereo/7.1 endpoint to be checked uniformly.
            mono /= _channels;
            _energy += mono * mono;
            _peak = Math.Max(_peak, Math.Abs(mono));

            // Maintain the sample index across WASAPI callback boundaries.
            // Both sine and cosine projections are necessary because the
            // loopback stream starts at an unknown phase relative to playback.
            double angle = (_frames % _sampleRate) * _radiansPerFrame;
            _projectionSin += mono * Math.Sin(angle);
            _projectionCos += mono * Math.Cos(angle);
            _frames++;
        }
    }

    public ToneEvidence Evaluate()
    {
        if (_frames == 0)
            return new ToneEvidence(0, 0, 0, 0, 0, false);
        double rms = Math.Sqrt(_energy / _frames);
        double matchedAmplitude = (2.0 / _frames) *
            Math.Sqrt(_projectionSin * _projectionSin +
                      _projectionCos * _projectionCos);
        // A sine wave recorded continuously has coherence near 1. Partial
        // silence around the stimulus reduces it (roughly sqrt(duty cycle)).
        double coherence = rms > 0
            ? matchedAmplitude / (Math.Sqrt(2) * rms)
            : 0;
        bool pass = _frames >= _sampleRate / 2 &&
                    rms >= 0.005 &&
                    _peak >= 0.012 &&
                    matchedAmplitude >= 0.007 &&
                    coherence >= 0.50 &&
                    _peak <= 0.15;
        return new ToneEvidence(_frames, rms, _peak, matchedAmplitude,
            coherence, pass);
    }

    internal readonly record struct ToneEvidence(
        long Frames, double Rms, double Peak, double Matched440Amplitude,
        double Coherence, bool Passed);

    /// <summary>
    /// Run device-free positive and adversarial regression checks. Their
    /// results are numeric only and must not be described as driver testing.
    /// </summary>
    public static void RunSelfTests()
    {
        foreach (int rate in new[] { 44100, 48000, 96000 })
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(rate, 2);
            ToneEvidence valid = Generate(format, 440, 0.025f, addSilence: true);
            if (!valid.Passed || valid.Coherence < 0.72 ||
                valid.Matched440Amplitude < 0.014)
                throw new InvalidOperationException(
                    $"440 Hz 2-channel callback-stream proof failed at {rate}: {valid}");
            if (Generate(format, 880, 0.025f, true).Passed)
                throw new InvalidOperationException("880 Hz was mistaken for 440 Hz.");
            if (Generate(format, 0, 0.025f, true).Passed)
                throw new InvalidOperationException("DC offset was mistaken for 440 Hz.");
            if (Generate(format, 0, 0, true).Passed)
                throw new InvalidOperationException("Silence was mistaken for 440 Hz.");
            if (Generate(format, 440, 0.025f, false, framesLimit: rate / 8).Passed)
                throw new InvalidOperationException("Insufficient frames were accepted.");
        }

        var f = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var verifier = new LoopbackToneVerifier(f);
        try
        {
            verifier.Add(new byte[5]);
            throw new InvalidOperationException("Misaligned packet was accepted.");
        }
        catch (InvalidDataException) { }

        var nonfinite = new byte[8];
        BinaryPrimitives.WriteSingleLittleEndian(nonfinite, float.NaN);
        try
        {
            verifier.Add(nonfinite);
            throw new InvalidOperationException("NaN PCM was accepted.");
        }
        catch (InvalidDataException) { }
        Console.WriteLine(
            "PASS: phase-insensitive 440 Hz WASAPI loopback identity check, " +
            "44.1/48/96 kHz streaming chunks, reject 880 Hz/DC/silence/short/NaN.");
    }

    private static ToneEvidence Generate(WaveFormat mix, int toneHz, float amplitude,
        bool addSilence, int? framesLimit = null)
    {
        var verifier = new LoopbackToneVerifier(mix);
        int limit = framesLimit ?? (addSilence ? mix.SampleRate * 3 : mix.SampleRate);
        int written = 0;
        int tick = 0;
        while (written < limit)
        {
            // Fragment at deliberately irregular frame boundaries. Streaming
            // projections must NOT reset phase between input callbacks.
            int frames = Math.Min(limit - written, new[] { 127, 511, 89, 1039, 31 }[tick++ % 5]);
            var packet = new byte[frames * mix.BlockAlign];
            for (int frame = 0; frame < frames; frame++)
            {
                int absoluteFrame = written + frame;
                bool withinStimulus = !addSilence ||
                    (absoluteFrame >= mix.SampleRate / 2 &&
                     absoluteFrame < mix.SampleRate * 5 / 2);
                float value = !withinStimulus ? 0f :
                    toneHz == 0 ? amplitude :
                    amplitude * MathF.Sin(
                        2f * MathF.PI * toneHz * absoluteFrame / mix.SampleRate);
                for (int ch = 0; ch < mix.Channels; ch++)
                    BinaryPrimitives.WriteSingleLittleEndian(
                        packet.AsSpan(frame * mix.BlockAlign + ch * sizeof(float)),
                        value);
            }
            verifier.Add(packet);
            written += frames;
        }
        return verifier.Evaluate();
    }
}
