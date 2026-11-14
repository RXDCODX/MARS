namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Управление глушением TTS.
/// </summary>
/// <remarks>
/// Глушением управляет координатор в <c>MARS.Alerts</c>: синтез для него —
/// одна из ног наравне с микшером и плеером.
/// </remarks>
public interface ITtsControlClient
{
    /// <summary>
    /// Выставляет состояние глушения и рассылает его подписчикам TTS.
    /// </summary>
    /// <returns><see langword="false"/>, если сервис недоступен или отказал.</returns>
    Task<bool> SetMuteStateAsync(
        bool isMuted,
        double volume,
        CancellationToken cancellationToken = default
    );
}
