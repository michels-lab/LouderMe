using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Quantitative native-DSP A/B evidence at the SAME explicitly selected
/// physical WASAPI endpoint. No virtual driver and no application capture.
/// This proves loopback amplitude comparison only, NOT acoustic SPL,
/// third-party global Boost, or trusted driver installation.
/// </summary>
internal static class DirectPhysicalGainComparison
{
    internal readonly record struct Result(
        bool Passed, string Code, double AmplitudeRatio);

    internal static Result Evaluate(
        LoopbackToneVerifier.ToneEvidence unity,
        LoopbackToneVerifier.ToneEvidence boosted)
    {
        // A 440-Hz identity alone is insufficient. Two individually valid
        // captures must have comparable durations and a real gain change.
        if (!unity.Passed || !boosted.Passed ||
            unity.Frames < 1 || boosted.Frames < 1 ||
            !double.IsFinite(unity.Matched440Amplitude) ||
            !double.IsFinite(boosted.Matched440Amplitude) ||
            !double.IsFinite(unity.Coherence) ||
            !double.IsFinite(boosted.Coherence) ||
            unity.Matched440Amplitude <= 0 ||
            boosted.Matched440Amplitude <= 0)
            return new(false, "INVALID_PHASE_EVIDENCE", double.NaN);

        double frameRatio = (double)boosted.Frames / unity.Frames;
        if (!double.IsFinite(frameRatio) || frameRatio is < 0.90 or > 1.10)
            return new(false, "INCOMPARABLE_MONITOR_DURATION", double.NaN);
        if (unity.Coherence < 0.50 || boosted.Coherence < 0.50)
            return new(false, "PHASE_COHERENCE_TOO_LOW", double.NaN);

        double ratio = boosted.Matched440Amplitude / unity.Matched440Amplitude;
        if (!double.IsFinite(ratio))
            return new(false, "INVALID_GAIN_RATIO", double.NaN);
        if (ratio is < 1.25 or > 1.75)
            return new(false, "PHYSICAL_LOOPBACK_GAIN_OUT_OF_RANGE", ratio);
        return new(true, "PHYSICAL_LOOPBACK_NATIVE_GAIN_AB_PASS", ratio);
    }

    internal static int Run(MMDevice physical)
    {
        ArgumentNullException.ThrowIfNull(physical);
        if (physical.State != DeviceState.Active ||
            physical.FriendlyName.Contains("LouderMe Virtual",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Gain A/B lab needs one explicitly selected active physical output.");

        // No Equalizer APO, no DSP EQ, no selection of Windows defaults.
        // Each 2 s tone is bounded to 0.025 float input and 100/150 Boost.
        // Run() waits for physical monitor shutdown BEFORE invoking callback.
        LoopbackToneVerifier.ToneEvidence unity = default;
        LoopbackToneVerifier.ToneEvidence boosted = default;
        Console.WriteLine("DIRECT_GAIN_AB: PHASE 100% START");
        if (DirectPhysicalDspProbe.Run(physical, 100, null,
                evidence => unity = evidence) != 0)
        {
            Console.Error.WriteLine(
                "FAIL: 100% physical loopback stimulus had no valid evidence.");
            return 4;
        }

        // A quiet separator is for lab clarity, not a pre/post negative
        // source control. No playback after a cancellation/error.
        Thread.Sleep(TimeSpan.FromMilliseconds(500));
        if (physical.State != DeviceState.Active)
            throw new IOException("Selected physical output became inactive between A/B phases.");

        Console.WriteLine("DIRECT_GAIN_AB: PHASE 150% START");
        if (DirectPhysicalDspProbe.Run(physical, 150, null,
                evidence => boosted = evidence) != 0)
        {
            Console.Error.WriteLine(
                "FAIL: 150% physical loopback stimulus had no valid evidence.");
            return 4;
        }

        var result = Evaluate(unity, boosted);
        Console.WriteLine(
            $"DIRECT_GAIN_AB: baseMatched440={unity.Matched440Amplitude:F6} " +
            $"boostedMatched440={boosted.Matched440Amplitude:F6} " +
            $"ratio={result.AmplitudeRatio:F4} " +
            $"baseMonitorFrames={unity.Frames} " +
            $"boostedMonitorFrames={boosted.Frames} " +
            $"status={result.Code}");
        if (!result.Passed)
        {
            Console.Error.WriteLine(
                "FAIL: physical WASAPI loopback has no comparable 100%->150% " +
                "amplitude rise. No native gain proof.");
            return 4;
        }
        Console.WriteLine(
            "PASS: 100%->150% native DSP produced a comparable measured " +
            "440-Hz amplitude rise on the same physical WASAPI loopback. " +
            "NOT proof of audible SPL, OS-wide effects, driver signing, " +
            "independent third-party app capture, or pre/post silence controls.");
        return 0;
    }

    internal static void RunSelfTests()
    {
        // These use the ACTUAL native C++ DSP and PCM adapter, but cannot
        // demonstrate any physical sound hardware without a user lab.
        var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        static LoopbackToneVerifier.ToneEvidence SyntheticEvidence(
            WaveFormat mix, int boost)
        {
            var pcm = DirectPhysicalDspProbe.PreparePcm(mix, boost);
            var verifier = new LoopbackToneVerifier(mix);
            verifier.Add(pcm);
            return verifier.Evaluate();
        }
        var baseEvidence = SyntheticEvidence(format, 100);
        var boostedEvidence = SyntheticEvidence(format, 150);
        var good = Evaluate(baseEvidence, boostedEvidence);
        if (!good.Passed || good.AmplitudeRatio is < 1.3 or > 1.7)
            throw new InvalidOperationException(
                "Owned C++ DSP 100/150 physical-format candidate did not meet gain A/B contract.");

        if (Evaluate(baseEvidence, baseEvidence).Passed ||
            Evaluate(boostedEvidence, baseEvidence).Passed)
            throw new InvalidOperationException(
                "No-gain/reversed-gain was incorrectly accepted.");
        if (Evaluate(default, boostedEvidence).Passed ||
            Evaluate(baseEvidence, default).Passed)
            throw new InvalidOperationException(
                "Missing phase was incorrectly accepted.");
        if (Evaluate(baseEvidence,
                boostedEvidence with { Frames = boostedEvidence.Frames / 2 }).Passed)
            throw new InvalidOperationException(
                "Incomparable physical capture durations were accepted.");
        if (Evaluate(baseEvidence,
                boostedEvidence with { Matched440Amplitude = double.NaN }).Passed ||
            Evaluate(baseEvidence,
                boostedEvidence with { Matched440Amplitude = double.PositiveInfinity }).Passed)
            throw new InvalidOperationException(
                "Nonfinite 440-Hz amplitude was accepted.");
        if (Evaluate(baseEvidence,
                boostedEvidence with { Coherence = 0.49 }).Passed)
            throw new InvalidOperationException(
                "Low-coherence physical monitor was accepted.");
        if (Evaluate(baseEvidence,
                boostedEvidence with {
                    Matched440Amplitude = baseEvidence.Matched440Amplitude * 3
                }).Passed)
            throw new InvalidOperationException(
                "Excessive unexplained gain was incorrectly accepted.");

        Console.WriteLine(
            "PASS: native C++ 100/150 boost A/B candidate ratio; reject " +
            "no change, reversed gain, missing phase, invalid durations, " +
            "nonfinite amplitude, low coherence and implausible gain. " +
            "No real output hardware was used.");
    }
}
