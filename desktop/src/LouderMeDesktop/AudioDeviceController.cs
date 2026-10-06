using NAudio.CoreAudioApi;

namespace LouderMeDesktop;

internal sealed class AudioDeviceController : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;

    public string DeviceName => Device.FriendlyName;

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
        _device?.Dispose();
        _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    private MMDevice Device => _device ??= _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

    public void Dispose()
    {
        _device?.Dispose();
        _enumerator.Dispose();
    }
}
