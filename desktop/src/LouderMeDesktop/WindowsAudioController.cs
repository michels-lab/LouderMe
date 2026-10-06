using NAudio.CoreAudioApi;

namespace LouderMeDesktop;

internal sealed class WindowsAudioController : IDisposable
{
    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _device;

    public string DeviceName
    {
        get
        {
            EnsureDevice();
            return _device?.FriendlyName ?? "No active output";
        }
    }

    public int VolumePercent
    {
        get
        {
            EnsureDevice();
            if (_device is null) return 0;
            return (int)Math.Round(_device.AudioEndpointVolume.MasterVolumeLevelScalar * 100.0f);
        }
        set
        {
            EnsureDevice();
            if (_device is null) return;
            _device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(value, 0, 100) / 100.0f;
        }
    }

    public bool IsMuted
    {
        get
        {
            EnsureDevice();
            return _device?.AudioEndpointVolume.Mute ?? false;
        }
        set
        {
            EnsureDevice();
            if (_device is not null) _device.AudioEndpointVolume.Mute = value;
        }
    }

    public void RefreshDefaultDevice()
    {
        _device?.Dispose();
        _device = null;
        EnsureDevice();
    }

    private void EnsureDevice()
    {
        if (_device is not null) return;
        _enumerator ??= new MMDeviceEnumerator();
        _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public void Dispose()
    {
        _device?.Dispose();
        _enumerator?.Dispose();
    }
}
