namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Вброс события оверлея в сервис, который им владеет.
/// </summary>
/// <remarks>
/// Широковещатель событий оверлея лежит в памяти <c>MARS.Alerts</c>, поэтому из
/// другого процесса событие доставляется вызовом наружу.
/// </remarks>
public interface IOverlayEventsClient
{
    /// <summary>
    /// Рассылает событие подписчикам оверлея.
    /// </summary>
    /// <param name="eventJson">Событие в JSON-форме protobuf-сообщения.</param>
    /// <returns><see langword="false"/>, если сервис недоступен или отказал.</returns>
    Task<bool> FireAsync(string eventJson, CancellationToken cancellationToken = default);
}
