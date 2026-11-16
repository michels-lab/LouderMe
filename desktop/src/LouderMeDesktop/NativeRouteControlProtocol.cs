using System.Globalization;

namespace LouderMeDesktop;

// Local, opt-in bridge standard input/output protocol. Receipt acknowledgement
// means native DSP setters ran on the WASAPI capture callback (not that the
// output device or a third-party app produced audible sound).
internal enum NativeControlReceiptKind { Queued, Applied, Rejected }

internal readonly record struct NativeControlReceipt(long Id, NativeControlReceiptKind Kind);

internal static class NativeRouteControlProtocol
{
    internal static string Tag(long id, string action)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(action) || action.Length > 180 ||
            action != action.Trim() || action.Contains('\n') || action.Contains('\r'))
            throw new ArgumentException("Unsafe native DSP command.");
        return "control " + id.ToString(CultureInfo.InvariantCulture) + " " + action;
    }

    internal static bool TryParseReceipt(string? line, out NativeControlReceipt receipt)
    {
        receipt = default;
        if (line is null || line.Length is < 22 or > 64) return false;

        const string queued = "NATIVE_CONTROL_QUEUED id=";
        const string applied = "NATIVE_CONTROL_APPLIED id=";
        const string rejected = "NATIVE_CONTROL_REJECTED id=";
        var kind = NativeControlReceiptKind.Queued;
        int offset;
        if (line.StartsWith(queued, StringComparison.Ordinal))
        {
            offset = queued.Length;
        }
        else if (line.StartsWith(applied, StringComparison.Ordinal))
        {
            offset = applied.Length;
            kind = NativeControlReceiptKind.Applied;
        }
        else if (line.StartsWith(rejected, StringComparison.Ordinal))
        {
            offset = rejected.Length;
            kind = NativeControlReceiptKind.Rejected;
        }
        else return false;

        if (!long.TryParse(line[offset..], NumberStyles.None,
            CultureInfo.InvariantCulture, out long id) || id <= 0)
            return false;
        receipt = new NativeControlReceipt(id, kind);
        return true;
    }

    internal static void RunContractSelfTests()
    {
        if (Tag(17, "boost 300") != "control 17 boost 300" ||
            !TryParseReceipt("NATIVE_CONTROL_QUEUED id=17", out var queued) ||
            queued.Kind != NativeControlReceiptKind.Queued || queued.Id != 17 ||
            !TryParseReceipt("NATIVE_CONTROL_APPLIED id=17", out var applied) ||
            applied.Kind != NativeControlReceiptKind.Applied || applied.Id != 17 ||
            !TryParseReceipt("NATIVE_CONTROL_REJECTED id=17", out var rejected) ||
            rejected.Kind != NativeControlReceiptKind.Rejected || rejected.Id != 17)
            throw new InvalidOperationException("Tagged native command handshake protocol regressed.");

        foreach (var invalid in new[] {
            "", "QUEUED for audio callback: Boost 300%",
            "NATIVE_CONTROL_APPLIED id=0", "NATIVE_CONTROL_APPLIED id=-1",
            "NATIVE_CONTROL_APPLIED id=+1",
            "NATIVE_CONTROL_APPLIED id=9999999999999999999999",
            "NATIVE_CONTROL_APPLIED id=1 more",
            "NATIVE_CONTROL_QUEUED id=abc",
            "NATIVE_PCM_ROUTE_STARTED",
            "NATIVE_CONTROL_APPLIED id=17\n",
            "NATIVE_CONTROL_SUPERSEDED id=17"
        })
            if (TryParseReceipt(invalid, out _))
                throw new InvalidOperationException("Forged or malformed native receipt accepted.");

        foreach (var action in new[] { "", " reset", "reset ", "reset\nstop" })
        {
            try { Tag(1, action); }
            catch (ArgumentException) { continue; }
            throw new InvalidOperationException("Unsafe tagged native control accepted.");
        }
        try { Tag(0, "reset"); }
        catch (ArgumentOutOfRangeException) { return; }
        throw new InvalidOperationException("Zero native receipt ID accepted.");
    }
}
