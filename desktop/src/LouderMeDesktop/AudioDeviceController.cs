using NAudio.CoreAudioApi;

namespace LouderMeDesktop;

internal sealed class AudioDeviceController : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;

    public string DeviceName => Device.FriendlyName;
    public string DeviceId => Device.ID;

    public int VolumePercent
    {
        get => (int)Math.Round(Device.AudioEndpointVolume.MasterVolumeLevelScalar * 100d);
        set => Device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(value, 0, 100) / 100f;
    }

    public bool Muted
    {
        get => Device.AudioEndpointVolume.Mute;
        set => Device.AudioEndpointVolume.Mute = value;
    }

    public void Refresh()
    {
        // Never keep a disconnected endpoint available to a later slider
        // write after the system reports no default media output.
        MMDevice next;
        try
        {
            next = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch
        {
            _device?.Dispose();
            _device = null;
            throw;
        }
        var old = _device;
        _device = next;
        if (old is not null && !ReferenceEquals(old, next))
            old.Dispose();
    }

    // A trackbar click may arrive between device-refresh ticks. Bind each
    // actual write to the exact endpoint identity displayed when the user
    // interacted; a new default device must not inherit a stale slider event.
    public bool TrySetVolumeForOutput(string? expectedId, int percent)
    {
        Refresh();
        if (!OutputRouteEvidencePolicy.SafeToWrite(expectedId, _device!.ID))
            return false;
        _device.AudioEndpointVolume.MasterVolumeLevelScalar =
            Math.Clamp(percent, 0, 100) / 100f;
        return true;
    }

    public bool TrySetMuteForOutput(string? expectedId, bool mute)
    {
        Refresh();
        if (!OutputRouteEvidencePolicy.SafeToWrite(expectedId, _device!.ID))
            return false;
        _device.AudioEndpointVolume.Mute = mute;
        return true;
    }

    private MMDevice Device => _device ??= _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

    public void Dispose()
    {
        _device?.Dispose();
        _enumerator.Dispose();
    }
}


/// <summary>
/// Hearing a short -18dB test is user-reported evidence for one exact output.
/// It cannot be carried to speakers, headphones or Bluetooth selected later.
/// </summary>
internal static class OutputRouteEvidencePolicy
{
    public static bool Changed(string? previous, string? current) =>
        !string.IsNullOrWhiteSpace(previous) &&
        !string.Equals(previous, current, StringComparison.Ordinal);

    public static bool SafeToWrite(string? displayedOutputId, string? nowOutputId) =>
        !string.IsNullOrWhiteSpace(displayedOutputId) &&
        string.Equals(displayedOutputId, nowOutputId, StringComparison.Ordinal);

    public static bool SameVerifiedOutput(
        string? probedDevice, string? nowDevice,
        long startingRevision, long nowRevision) =>
        !string.IsNullOrWhiteSpace(probedDevice) &&
        string.Equals(probedDevice, nowDevice, StringComparison.Ordinal) &&
        startingRevision == nowRevision;

    public static void RunSelfTests()
    {
        if (Changed(null, "A") || Changed("A", "A") ||
            !Changed("A", "B") || !Changed("A", null) ||
            !SameVerifiedOutput("A", "A", 1, 1) ||
            SameVerifiedOutput("A", "B", 1, 1) ||
            SameVerifiedOutput("A", "A", 1, 3) ||
            SameVerifiedOutput(null, "A", 1, 1) ||
            !SafeToWrite("A", "A") ||
            SafeToWrite("A", "B") ||
            SafeToWrite(null, "A") ||
            SafeToWrite("", "A") ||
            SafeToWrite("A", null) ||
            SafeToWrite("a", "A"))
            throw new InvalidOperationException(
                "Output change incorrectly retained an old physical listening test.");
        Console.WriteLine(
            "PASS: output identity, unplug, A->B and A->B->A stale-evidence rejection, route-bound user volume writes.");
    }
}
