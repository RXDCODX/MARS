namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Управление глушением плеера SoundRequest.
/// </summary>
/// <remarks>
/// Глушением управляет координатор в <c>MARS.Alerts</c>: плеер для него — одна
/// из ног наравне с микшером и синтезом.
/// </remarks>
public interface ISoundRequestControlClient
{
    /// <summary>
    /// Глушит плеер и приостанавливает воспроизведение.
    /// </summary>
    /// <returns><see langword="false"/>, если сервис недоступен или отказал.</returns>
    Task<bool> MuteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Снимает mute с плеера и возобновляет воспроизведение, если оно было
    /// приостановлено именно этим mute.
    /// </summary>
    /// <returns><see langword="false"/>, если сервис недоступен или отказал.</returns>
    Task<bool> UnmuteAllAsync(CancellationToken cancellationToken = default);
}
