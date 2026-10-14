namespace MARS.Shared.Models.Audio;

/// <summary>
/// Результат установки громкости микрофона: предыдущее и текущее значение.
/// </summary>
public sealed class MicrophoneVolumeSetResponse
{
    public float PreviousVolume { get; set; }

    public string PreviousVolumePercent { get; set; } = string.Empty;

    public float CurrentVolume { get; set; }

    public string CurrentVolumePercent { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
