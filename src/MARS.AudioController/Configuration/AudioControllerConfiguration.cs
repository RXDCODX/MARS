namespace MARS.AudioController.Configuration;

/// <summary>
/// Настройки MARS.AudioController: адрес для внешних клиентов и список процессов,
/// которые не попадают под mute микшера по умолчанию.
/// </summary>
public class AudioControllerConfiguration
{
    public const string SectionName = "AudioController";

    /// <summary>
    /// Базовый адрес сервиса. Используется клиентами для логирования и как
    /// значение по умолчанию, когда адрес не передан явно.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:30695";

    /// <summary>
    /// Процессы, которые исключаются из mute микшера, если вызывающий не передал
    /// свой список. По умолчанию — OBS: его браузерный источник должен остаться
    /// слышимым, чтобы алерт не заглушил сам себя.
    /// </summary>
    public IReadOnlyList<string> DefaultExcludedProcessNames { get; set; } =
    ["obs64", "obs32", "obs-browser-page"];
}
