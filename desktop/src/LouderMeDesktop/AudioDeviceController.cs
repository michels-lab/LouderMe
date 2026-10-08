using NAudio.CoreAudioApi;

namespace LouderMeDesktop;

internal sealed record DeviceVolumeSnapshot(
    string DeviceId,
    string DeviceName,
    int VolumePercent,
    bool Muted);

internal sealed class AudioDeviceController : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;

    public DeviceVolumeSnapshot ReadSnapshot()
    {
        var device = EnsureCurrentDevice();
        return new DeviceVolumeSnapshot(
            device.ID,
            device.FriendlyName,
            (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100d),
            device.AudioEndpointVolume.Mute);
    }

    public int VolumePercent
    {
        get => ReadSnapshot().VolumePercent;
        set
        {
            var device = EnsureCurrentDevice();
            device.AudioEndpointVolume.MasterVolumeLevelScalar =
                Math.Clamp(value, 0, 100) / 100f;
        }
    }

    public bool Muted
    {
        get => ReadSnapshot().Muted;
        set => EnsureCurrentDevice().AudioEndpointVolume.Mute = value;
    }

    public void Refresh()
    {
        ReplaceWithDefaultDevice();
    }

    private MMDevice EnsureCurrentDevice()
    {
        using var currentDefault =
            _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        if (_device is null ||
            !string.Equals(_device.ID, currentDefault.ID, StringComparison.Ordinal))
        {
            _device?.Dispose();
            _device = _enumerator.GetDevice(currentDefault.ID);
        }

        return _device;
    }

    private void ReplaceWithDefaultDevice()
    {
        _device?.Dispose();
        _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public void Dispose()
    {
        _device?.Dispose();
        _enumerator.Dispose();
    }
}
