namespace MARS.Shared.Contracts.Models.Audio;

/// <summary>
/// Состояние громкости микрофона.
/// </summary>
public sealed class MicrophoneVolumeResponse
{
    public float Volume { get; set; }

    public string VolumePercent { get; set; } = string.Empty;

    public bool IsMuted { get; set; }

    public bool IsAtMaximum { get; set; }

    public string DeviceName { get; set; } = string.Empty;
}
