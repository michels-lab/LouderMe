using NAudio.Wave;

namespace LouderMeAudioBridge;

/// <summary>
/// Read-only safety preflight for a user-selected virtual-to-physical route.
/// A READY result only means the observed endpoint metadata satisfies the
/// bridge's static constraints. It does NOT prove a signed driver, non-silent
/// virtual capture, native DSP playback, or sound at the speakers.
/// </summary>
internal static class RoutePreflight
{
    internal sealed record Endpoint(
        string Id, string Name, WaveFormat? Format, bool Active);

    internal sealed record Result(bool Ready, string Code, string Explanation);

    private static Result Reject(string code, string explanation) =>
        new(false, code, explanation);

    public static Result Assess(
        IReadOnlyCollection<Endpoint> endpoints, string? sourceId, string? sinkId)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        if (endpoints.Any(x => x is null || string.IsNullOrWhiteSpace(x.Id)))
            return Reject("INVALID_ENDPOINT_SNAPSHOT",
                "Windows returned an incomplete audio-device identity.");

        if (!endpoints.Any(x => x.Active &&
            x.Name.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase)))
            return Reject("VIRTUAL_ENDPOINT_NOT_FOUND",
                "No active LouderMe Virtual render endpoint is installed. Do not use a physical output as capture source.");

        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(sinkId))
            return Reject("SELECTION_REQUIRED",
                "Select the exact source and physical output IDs using --list-devices; defaults are never changed.");

        if (string.Equals(sourceId, sinkId, StringComparison.Ordinal))
            return Reject("SAME_ENDPOINT",
                "Virtual capture and physical output must use different Windows endpoint IDs.");

        var sources = endpoints.Where(x =>
            string.Equals(x.Id, sourceId, StringComparison.Ordinal)).Take(2).ToArray();
        var sinks = endpoints.Where(x =>
            string.Equals(x.Id, sinkId, StringComparison.Ordinal)).Take(2).ToArray();
        if (sources.Length != 1 || sinks.Length != 1)
            return Reject("ENDPOINT_SELECTION_MISSING_OR_AMBIGUOUS",
                "One or both selected endpoint IDs are missing or ambiguous; do not substitute defaults.");

        var source = sources[0];
        var sink = sinks[0];
        if (!source.Active || !sink.Active)
            return Reject("ENDPOINT_INACTIVE",
                "A selected Windows endpoint is not active.");
        if (!source.Name.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase))
            return Reject("SOURCE_NOT_VIRTUAL",
                "Capture source must be the selected LouderMe Virtual render endpoint.");
        if (sink.Name.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase))
            return Reject("SINK_NOT_PHYSICAL",
                "The sink must be a distinct physical render endpoint, never another LouderMe Virtual endpoint.");
        if (source.Format is null || sink.Format is null)
            return Reject("MIX_FORMAT_UNAVAILABLE",
                "Windows did not expose the selected source/output mix formats.");
        if (!PcmFormatAdapter.IsSupported(source.Format) ||
            !PcmFormatAdapter.IsSupported(sink.Format))
            return Reject("UNSUPPORTED_MIX_FORMAT",
                "Both selected endpoints must use PCM16 or IEEE float32 with 1–8 channels at 8–192 kHz.");

        try
        {
            // Reuse the REAL converter's acceptance contract. Read-only
            // format construction does not open WASAPI streams or install DSP.
            _ = new PcmFormatAdapter(source.Format, sink.Format);
        }
        catch (NotSupportedException)
        {
            return Reject("INCOMPATIBLE_ROUTE_FORMATS",
                "Sample-rate ratio or channel mapping cannot be handled safely by this bridge.");
        }

        return new(true, "STATIC_PREFLIGHT_READY",
            "Selected endpoint metadata is compatible for an explicitly consented LAB attempt only; actual audio, driver signing, loopback, DSP effect and SPL are unverified.");
    }

    public static void RunSelfTests()
    {
        var stereo = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var mono = new WaveFormat(44100, 16, 1);
        var virtualSource = new Endpoint("virtual-A", "LouderMe Virtual (Lab)", stereo, true);
        var physical = new Endpoint("physical-B", "Speakers", mono, true);
        Endpoint[] pair = [virtualSource, physical];

        static void Expect(string expected, IReadOnlyCollection<Endpoint> items,
            string? source, string? sink)
        {
            var actual = Assess(items, source, sink);
            if (actual.Code != expected || actual.Ready !=
                (expected == "STATIC_PREFLIGHT_READY"))
                throw new InvalidOperationException(
                    "Route preflight expected " + expected + " but got " + actual.Code);
        }

        Expect("STATIC_PREFLIGHT_READY", pair, "virtual-A", "physical-B");
        Expect("SELECTION_REQUIRED", pair, null, "physical-B");
        Expect("SELECTION_REQUIRED", pair, "virtual-A", null);
        Expect("SAME_ENDPOINT", pair, "virtual-A", "virtual-A");
        Expect("VIRTUAL_ENDPOINT_NOT_FOUND", [physical], "virtual-A", "physical-B");
        Expect("ENDPOINT_SELECTION_MISSING_OR_AMBIGUOUS", pair, "not-found", "physical-B");
        Expect("ENDPOINT_SELECTION_MISSING_OR_AMBIGUOUS",
            [virtualSource, physical, physical], "virtual-A", "physical-B");
        Expect("SOURCE_NOT_VIRTUAL", pair, "physical-B", "virtual-A");
        Expect("SINK_NOT_PHYSICAL",
            [virtualSource, new Endpoint("virtual-B", "LouderMe Virtual (Lab)", stereo, true)],
            "virtual-A", "virtual-B");
        Expect("ENDPOINT_INACTIVE",
            [virtualSource, physical with { Active = false }],
            "virtual-A", "physical-B");
        Expect("MIX_FORMAT_UNAVAILABLE",
            [virtualSource with { Format = null }, physical],
            "virtual-A", "physical-B");
        Expect("UNSUPPORTED_MIX_FORMAT",
            [virtualSource, physical with {
                Format = new WaveFormat(48000, 24, 2)
            }], "virtual-A", "physical-B");
        Expect("INCOMPATIBLE_ROUTE_FORMATS",
            [virtualSource, physical with {
                Format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 6)
            }], "virtual-A", "physical-B");
        Expect("INCOMPATIBLE_ROUTE_FORMATS",
            [virtualSource with {
                Format = WaveFormat.CreateIeeeFloatWaveFormat(192000, 2)
            }, physical with {
                Format = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2)
            }], "virtual-A", "physical-B");
        Expect("INVALID_ENDPOINT_SNAPSHOT",
            [virtualSource with { Id = "" }, physical], "virtual-A", "physical-B");

        // No hardware or PnP/volume settings touched; no success banner may
        // imply signed, installed, audible, or third-party route proof.
        Console.WriteLine(
            "PASS: read-only route preflight rejects missing/ambiguous/inactive " +
            "endpoints, feedback, unsafe PCM formats and unsupported channel/rate " +
            "maps; compatible metadata is NOT proof of audible Windows sound.");
    }
}
