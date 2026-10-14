using System.Diagnostics;
using MARS.AudioController.Services;
using NAudio.CoreAudioApi;

namespace MARS.AudioController.Services.Naaudio;

/// <summary>
/// Реализация <see cref="IAudioDeviceService"/> на базе NAudio Core Audio API.
/// Работает только на Windows: активация COM-интерфейса IMMDeviceEnumerator
/// на других ОС бросает исключение.
/// </summary>
public sealed class NaaudioAudioDeviceService : IAudioDeviceService
{
    public Task<float> GetVolumeAsync(CancellationToken cancellationToken = default)
    {
        var device = GetDefaultDevice();
        var volume = device.AudioEndpointVolume.MasterVolumeLevelScalar;

        return Task.FromResult(volume);
    }

    public Task SetVolumeAsync(float volume, CancellationToken cancellationToken = default)
    {
        var device = GetDefaultDevice();
        device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(volume, 0f, 1f);
        device.AudioEndpointVolume.Mute = false;

        return Task.CompletedTask;
    }

    public Task SetVolumeToMaxAsync(CancellationToken cancellationToken = default)
    {
        var device = GetDefaultDevice();
        device.AudioEndpointVolume.MasterVolumeLevelScalar = 1f;
        device.AudioEndpointVolume.Mute = false;

        return Task.CompletedTask;
    }

    public Task<string> GetDeviceNameAsync(CancellationToken cancellationToken = default)
    {
        var device = GetDefaultDevice();

        return Task.FromResult(device.FriendlyName);
    }

    public Task<bool> IsMutedAsync(CancellationToken cancellationToken = default)
    {
        var device = GetDefaultDevice();

        return Task.FromResult(device.AudioEndpointVolume.Mute);
    }

    private static MMDevice GetDefaultDevice()
    {
        var enumerator = new MMDeviceEnumerator();
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
    }
}
