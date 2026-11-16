using System.Globalization;

namespace LouderMeDesktop;

/// <summary>
/// Conservative, read-only counters emitted by the actual Windows WASAPI bridge.
/// They measure transport and DSP callback activity, never audible sound, SPL,
/// a signed kernel driver, or support for arbitrary third-party applications.
/// Parsing happens on the stdout worker, not in an audio callback.
/// </summary>
internal sealed record NativeRouteTelemetry(
    long SourceFrames,
    long QueuedPhysicalFrames,
    long RenderConsumedFrames,
    long DroppedSourceFrames,
    long Overflows,
    long QueueResets,
    long StarvedRenderReads,
    long CommandsPublished,
    long CommandsApplied,
    long CommandsSuperseded)
{
    private static readonly string[] RequiredKeys =
    [
        "sourceFrames", "queuedPhysicalFrames", "renderConsumedFrames",
        "droppedSourceFrames", "overflows", "queueResets",
        "starvedRenderReads", "liveCommandsPublished",
        "liveCommandsApplied", "liveCommandsSuperseded"
    ];

    internal bool HasLoss => DroppedSourceFrames > 0 || Overflows > 0 || QueueResets > 0;

    internal string DisplayText =>
        "Captured PCM frames (may include silence): " + SourceFrames.ToString("N0", CultureInfo.InvariantCulture) +
        "\nQueued for physical render: " + QueuedPhysicalFrames.ToString("N0", CultureInfo.InvariantCulture) +
        "  |  Consumed by renderer: " + RenderConsumedFrames.ToString("N0", CultureInfo.InvariantCulture) +
        "\nDropped source frames: " + DroppedSourceFrames.ToString("N0", CultureInfo.InvariantCulture) +
        "  |  Overflows: " + Overflows.ToString("N0", CultureInfo.InvariantCulture) +
        "  |  Queue resets: " + QueueResets.ToString("N0", CultureInfo.InvariantCulture) +
        "\nUnderfilled renderer reads: " + StarvedRenderReads.ToString("N0", CultureInfo.InvariantCulture) +
        " (also expected during silence)" +
        "\nDSP commands: " + CommandsApplied.ToString("N0", CultureInfo.InvariantCulture) +
        " applied / " + CommandsPublished.ToString("N0", CultureInfo.InvariantCulture) +
        " published  |  " + CommandsSuperseded.ToString("N0", CultureInfo.InvariantCulture) +
        " superseded" +
        (HasLoss ? "\nWARNING: transport dropped or flushed PCM. Check the selected route." :
            SourceFrames == 0 ? "\nWaiting for virtual capture frames; route startup is not proof of sound." :
            "\nPCM transport observed; audibility and third-party playback NOT verified.");

    internal static bool TryParse(string? line, out NativeRouteTelemetry? sample)
    {
        sample = null;
        if (string.IsNullOrWhiteSpace(line) || line.Length > 1600 ||
            !line.StartsWith("sourceFrames=", StringComparison.Ordinal))
            return false;

        var counters = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var token in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var sep = token.IndexOf('=');
            if (sep <= 0) continue;
            var key = token[..sep];
            if (!RequiredKeys.Contains(key, StringComparer.Ordinal)) continue;
            if (counters.ContainsKey(key) ||
                !long.TryParse(token[(sep + 1)..], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var value) ||
                value < 0)
                return false;
            counters[key] = value;
        }
        if (counters.Count != RequiredKeys.Length) return false;
        sample = new NativeRouteTelemetry(
            counters["sourceFrames"], counters["queuedPhysicalFrames"],
            counters["renderConsumedFrames"], counters["droppedSourceFrames"],
            counters["overflows"], counters["queueResets"],
            counters["starvedRenderReads"], counters["liveCommandsPublished"],
            counters["liveCommandsApplied"], counters["liveCommandsSuperseded"]);
        return true;
    }

    internal static void RunContractSelfTests()
    {
        const string valid =
            "sourceFrames=48000 queuedPhysicalFrames=44100 renderConsumedFrames=43500 " +
            "droppedSourceFrames=0 queuedAudio=14.0ms (NOT acoustic latency) " +
            "clockCorrection=+0.0ppm callbackWorkToAudio=5.1% " +
            "worstCallback=1.20ms overBudgetCallbacks=0 starvedRenderReads=2 " +
            "overflows=0 queueResets=0 flushedOutputFrames=0 " +
            "liveCommandsPublished=3 liveCommandsApplied=2 liveCommandsSuperseded=1";
        if (!TryParse(valid, out var sample) || sample is null ||
            sample.SourceFrames != 48000 || sample.RenderConsumedFrames != 43500 ||
            sample.CommandsPublished != 3 || sample.CommandsApplied != 2 ||
            sample.CommandsSuperseded != 1 || sample.HasLoss ||
            !sample.DisplayText.Contains("NOT verified", StringComparison.Ordinal) ||
            sample.DisplayText.Contains("sound verified", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Native transport status valid parse/presentation failed.");
        var lossy = valid.Replace("droppedSourceFrames=0", "droppedSourceFrames=900");
        if (!TryParse(lossy, out var loss) || loss is null || !loss.HasLoss ||
            !loss.DisplayText.Contains("WARNING", StringComparison.Ordinal))
            throw new InvalidOperationException("PCM loss not visible in native transport status.");
        var silent = valid.Replace("sourceFrames=48000", "sourceFrames=0");
        if (!TryParse(silent, out var idle) || idle is null ||
            !idle.DisplayText.Contains("Waiting for virtual capture", StringComparison.Ordinal))
            throw new InvalidOperationException("No PCM falsely presented as working output.");
        foreach (var invalid in new[]
        {
            "", "NATIVE_PCM_ROUTE_STARTED", "sourceFrames=100",
            valid.Replace("sourceFrames=48000", "sourceFrames=-1"),
            valid.Replace("sourceFrames=48000", "sourceFrames=1.0"),
            valid.Replace("sourceFrames=48000", "sourceFrames=99999999999999999999999"),
            valid + " sourceFrames=1",
            valid.Replace("renderConsumedFrames=43500", "renderConsumedFrames=0xFF"),
            valid.Replace("liveCommandsApplied=2", "somethingElse=2"),
            valid + new string(' ', 1601)
        })
            if (TryParse(invalid, out _))
                throw new InvalidOperationException("Invalid or fabricated bridge telemetry accepted.");
    }
}
