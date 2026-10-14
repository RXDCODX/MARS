namespace MARS.AudioController.Services;

/// <summary>
/// Доступ к устройству захвата звука (микрофону). Абстракция над NAudio,
/// чтобы контроллеры были тестируемыми без реального аудиоустройства.
/// </summary>
public interface IAudioDeviceService
{
    /// <summary>
    /// Текущая громкость микрофона в диапазоне 0.0–1.0.
    /// </summary>
    Task<float> GetVolumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Устанавливает громкость микрофона. Значение ограничивается диапазоном 0.0–1.0.
    /// </summary>
    Task SetVolumeAsync(float volume, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает громкость микрофона к максимуму (1.0) и снимает mute.
    /// </summary>
    Task SetVolumeToMaxAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Имя устройства захвата по умолчанию.
    /// </summary>
    Task<string> GetDeviceNameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Заглушен ли микрофон на уровне ОС.
    /// </summary>
    Task<bool> IsMutedAsync(CancellationToken cancellationToken = default);
}
