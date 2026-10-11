using System.Buffers.Binary;

namespace LouderMeAudioBridge;

/// <summary>
/// Stateful, finite windowed-sinc anti-aliasing FIR for the experimental
/// Windows WASAPI bridge. Only enabled when output sample rate is lower
/// than the input sample rate. All history and coefficients are allocated
/// once at construction. The caller owns the PCM packet; this modifies it
/// in place before the streaming interpolation stage.
/// </summary>
internal sealed class StreamingDownsampleLowPass
{
    private readonly float[] _coefficients;
    private readonly float[] _history;
    private readonly int _channels;
    private readonly int _taps;
    private int _position;

    public int TapCount => _taps;

    public StreamingDownsampleLowPass(int inputRate, int outputRate, int channels)
    {
        if (inputRate is < 8000 or > 192000 ||
            outputRate is < 8000 or > 192000 ||
            outputRate >= inputRate || channels is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(outputRate),
                "Anti-alias FIR requires a valid downsampling ratio and 1–8 channels.");

        double ratio = (double)outputRate / inputRate;
        // Narrow transition bands near a 1:1 rate change need more taps.
        // Wider 2:1 and 4:1 conversions need fewer, reducing callback CPU.
        _taps = ratio >= 0.80 ? 127 : ratio >= 0.50 ? 63 : 47;
        _channels = channels;
        _history = new float[_taps * channels];
        _coefficients = new float[_taps];
        double cutoff = 0.45 * ratio; // cycles per input frame; output Nyquist is 0.5*ratio
        int midpoint = (_taps - 1) / 2;
        double sum = 0;
        for (int tap = 0; tap < _taps; tap++)
        {
            int offset = tap - midpoint;
            double sinc = offset == 0
                ? 2 * cutoff
                : Math.Sin(2 * Math.PI * cutoff * offset) / (Math.PI * offset);
            double hamming = 0.54 - 0.46 *
                Math.Cos(2 * Math.PI * tap / (_taps - 1));
            double value = sinc * hamming;
            _coefficients[tap] = (float)value;
            sum += value;
        }
        for (int tap = 0; tap < _taps; tap++)
            _coefficients[tap] = (float)(_coefficients[tap] / sum);
    }

    public void ProcessInPlace(byte[] packet, int byteCount)
    {
        if (byteCount < 0 || byteCount > packet.Length ||
            byteCount % (_channels * sizeof(float)) != 0)
            throw new ArgumentException("Expected complete interleaved float32 frames.");
        int frames = byteCount / (_channels * sizeof(float));
        for (int frame = 0; frame < frames; frame++)
        {
            int packetFrame = frame * _channels * sizeof(float);
            int historyFrame = _position * _channels;
            // Store the complete original input frame before replacing any
            // sample so channel history cannot depend on write order.
            for (int channel = 0; channel < _channels; channel++)
            {
                float sample = BinaryPrimitives.ReadSingleLittleEndian(
                    packet.AsSpan(packetFrame + channel * sizeof(float)));
                if (!float.IsFinite(sample))
                    throw new InvalidDataException("Nonfinite PCM before anti-alias filtering.");
                _history[historyFrame + channel] = sample;
            }

            for (int channel = 0; channel < _channels; channel++)
            {
                double filtered = 0;
                int index = _position;
                for (int tap = 0; tap < _taps; tap++)
                {
                    filtered += _coefficients[tap] *
                        _history[index * _channels + channel];
                    if (--index < 0) index = _taps - 1;
                }
                BinaryPrimitives.WriteSingleLittleEndian(
                    packet.AsSpan(packetFrame + channel * sizeof(float)),
                    (float)filtered);
            }
            if (++_position == _taps) _position = 0;
        }
    }
}
