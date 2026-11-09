using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Immutable format identities for the explicitly selected WASAPI source
/// and sink. Endpoint IDs can remain identical and Active after a settings
/// change to sample rate/bit depth/channel map; continuing the old PCM
/// adapter would corrupt channel identity or sample boundaries.
/// Console/management thread only; never reads COM on the audio callback.
/// </summary>
internal sealed class RouteFormatEpoch
{
    private readonly FormatStamp _source;
    private readonly FormatStamp _sink;

    private readonly record struct FormatStamp(
        int Rate, int Channels, int Bits, int BlockAlign,
        int AverageBytesPerSecond, WaveFormatEncoding Encoding,
        int ValidBits, int SpeakerMask, Guid Subtype, bool Extensible)
    {
        internal static FormatStamp From(WaveFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);
            var extension = format as WaveFormatExtensible;
            return new FormatStamp(
                format.SampleRate, format.Channels, format.BitsPerSample,
                format.BlockAlign, format.AverageBytesPerSecond,
                format.Encoding,
                extension?.ValidBitsPerSample ?? format.BitsPerSample,
                extension?.ChannelMask ?? 0,
                extension?.SubFormat ?? Guid.Empty,
                extension is not null);
        }
    }

    internal RouteFormatEpoch(WaveFormat source, WaveFormat sink)
    {
        _source = FormatStamp.From(source);
        _sink = FormatStamp.From(sink);
    }

    internal void EnsureUnchanged(WaveFormat source, WaveFormat sink)
    {
        if (FormatStamp.From(source) != _source ||
            FormatStamp.From(sink) != _sink)
            throw new AudioRouteRecoverableException(
                "Selected virtual/physical WASAPI mix format changed in-place. " +
                "Release the current audio engine and capture streams; " +
                "a new explicit route preflight is required before replay.");
    }

    public static void RunSelfTests()
    {
        static void Require(bool ok, string explanation)
        {
            if (!ok) throw new InvalidOperationException(explanation);
        }
        static void MustInvalidate(RouteFormatEpoch epoch, WaveFormat a,
            WaveFormat b, string label)
        {
            try
            {
                epoch.EnsureUnchanged(a, b);
                throw new InvalidOperationException("Stale PCM accepted: " + label);
            }
            catch (AudioRouteRecoverableException) { }
        }

        var left = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var right = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var epoch = new RouteFormatEpoch(left, right);
        epoch.EnsureUnchanged(
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 2),
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));

        MustInvalidate(epoch,
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2), right,
            "virtual rate");
        MustInvalidate(epoch, left,
            WaveFormat.CreateIeeeFloatWaveFormat(96000, 2),
            "physical rate");
        MustInvalidate(epoch,
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 1), right,
            "source channel count");
        MustInvalidate(epoch, left, new WaveFormat(48000, 16, 2),
            "sink encoding/bit depth");
        MustInvalidate(epoch,
            new WaveFormatExtensible(48000, 32, 2, true, 32, 0x3),
            right, "source wrapper switch");
        MustInvalidate(epoch, left,
            new WaveFormatExtensible(48000, 32, 2, true, 32, 0x3),
            "physical wrapper switch");

        var baseSurround = new WaveFormatExtensible(
            48000, 32, 6, true, 32, 0x3f);
        var sink = new WaveFormatExtensible(
            48000, 32, 2, true, 32, 0x3);
        var six = new RouteFormatEpoch(baseSurround, sink);
        six.EnsureUnchanged(
            new WaveFormatExtensible(48000, 32, 6, true, 32, 0x3f),
            new WaveFormatExtensible(48000, 32, 2, true, 32, 0x3));
        MustInvalidate(six,
            new WaveFormatExtensible(48000, 32, 6, true, 32, 0x60f),
            sink, "5.1 back-to-side speaker switch");
        MustInvalidate(six,
            new WaveFormatExtensible(48000, 32, 8, true, 32, 0x63f),
            sink, "5.1-to-7.1 switch");
        MustInvalidate(six, baseSurround,
            new WaveFormatExtensible(48000, 32, 2, false, 24, 0x3),
            "output integer PCM24/32 vs float32");
        MustInvalidate(six, baseSurround,
            new WaveFormatExtensible(48000, 32, 2, true, 32, 0xC),
            "physical stereo speaker positions");

        Console.WriteLine(
            "PASS: unchanged PCM endpoint epochs, source/sink rates, " +
            "channel counts, encodings, valid bits and surround masks; " +
            "device-free route invalidation before stale rendering.");
    }
}
