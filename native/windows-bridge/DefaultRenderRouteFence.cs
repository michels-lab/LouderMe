using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace LouderMeAudioBridge;

/// <summary>
/// Fail-closed routing fence for the opt-in virtual->physical WASAPI bridge.
/// A fresh default-device choice while both streams are alive can bypass
/// LouderMe DSP or double playback. We do not set Windows defaults: the user
/// must explicitly review the new route and launch a new session.
/// </summary>
internal sealed class DefaultRenderRouteFence
{
    internal readonly record struct Snapshot(
        string Console, string Multimedia, string Communications);

    private Snapshot? _pinned;

    internal void VerifyOrPin(Snapshot current)
    {
        if (string.IsNullOrWhiteSpace(current.Console) ||
            string.IsNullOrWhiteSpace(current.Multimedia) ||
            string.IsNullOrWhiteSpace(current.Communications))
            throw new DefaultRenderRouteChangedException(
                "One or more Windows default playback roles became unavailable. " +
                "Stop the bridge and confirm the selected endpoints again.");

        if (_pinned is null)
        {
            _pinned = current;
            return;
        }

        // Ordinal endpoint identities, NOT friendly names. A headphones
        // disconnect/reconnect can reuse a label but change its Windows ID.
        if (_pinned.Value != current)
            throw new DefaultRenderRouteChangedException(
                "Windows changed the Console, Multimedia or Communications " +
                "default playback endpoint. Stop all bridge audio; do not " +
                "automatically reconnect under a different default route.");
    }

    internal void VerifyOrPin(MMDeviceEnumerator enumerator)
    {
        ArgumentNullException.ThrowIfNull(enumerator);
        try
        {
            VerifyOrPin(new Snapshot(
                GetEndpointId(enumerator, Role.Console),
                GetEndpointId(enumerator, Role.Multimedia),
                GetEndpointId(enumerator, Role.Communications)));
        }
        catch (COMException ex)
        {
            throw new DefaultRenderRouteChangedException(
                "A Windows default playback endpoint became unreadable; " +
                "end experimental playback without automatic rerouting.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new DefaultRenderRouteChangedException(
                "Windows could not confirm an unchanged default playback route.", ex);
        }
    }

    private static string GetEndpointId(MMDeviceEnumerator enumerator, Role role)
    {
        using var endpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, role);
        if (endpoint is null || endpoint.State != DeviceState.Active ||
            string.IsNullOrWhiteSpace(endpoint.ID))
            throw new DefaultRenderRouteChangedException(
                "A selected Windows default playback role is missing or inactive.");
        return endpoint.ID;
    }

    internal static void RunSelfTests()
    {
        var fence = new DefaultRenderRouteFence();
        var original = new Snapshot("headphones-A", "headphones-A", "speaker-B");
        fence.VerifyOrPin(original);
        fence.VerifyOrPin(new Snapshot("headphones-A", "headphones-A", "speaker-B"));

        static void Reject(DefaultRenderRouteFence guard, Snapshot snapshot, string caseName)
        {
            try
            {
                guard.VerifyOrPin(snapshot);
            }
            catch (DefaultRenderRouteChangedException) { return; }
            throw new InvalidOperationException(
                "Default playback drift was not rejected: " + caseName);
        }

        Reject(fence, original with { Console = "headphones-C" }, "Console");
        Reject(fence, original with { Multimedia = "headphones-C" }, "Multimedia");
        Reject(fence, original with { Communications = "headphones-C" }, "Communications");
        Reject(fence, original with { Console = "HEADPHONES-A" }, "case-sensitive identity");
        Reject(fence, original with { Multimedia = "" }, "disconnected Multimedia");
        Reject(fence, original with { Communications = " " }, "missing Communications");

        // The fence deliberately survives a bounded same-endpoint retry.
        // Reopening WASAPI cannot reset the default-role baseline.
        fence.VerifyOrPin(original);
        Reject(fence, new Snapshot("headphones-A", "speaker-B", "speaker-B"),
            "attempted auto-reconnect after a new physical default");
        var independentSession = new DefaultRenderRouteFence();
        independentSession.VerifyOrPin(new Snapshot("new-A", "new-A", "new-B"));

        Console.WriteLine(
            "PASS: Windows default-render role fence pins Console/Multimedia/" +
            "Communications across reconnects and refuses hot-swap, " +
            "missing roles and endpoint-label aliasing; no audio devices used.");
    }
}

internal sealed class DefaultRenderRouteChangedException : IOException
{
    internal DefaultRenderRouteChangedException(string message) : base(message) { }
    internal DefaultRenderRouteChangedException(string message, Exception inner) :
        base(message, inner) { }
}
