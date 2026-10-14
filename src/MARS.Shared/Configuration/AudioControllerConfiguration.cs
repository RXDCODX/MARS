namespace MARS.Shared.Configuration;

/// <summary>
/// Настройки подключения к MARS.AudioController — сервису управления громкостью
/// микрофона и микшером приложений. Работает на Windows-хосте, вне Docker.
/// </summary>
public class AudioControllerConfiguration
{
    public const string SectionName = "AudioController";

    /// <summary>
    /// Базовый адрес сервиса.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:30695";
}
