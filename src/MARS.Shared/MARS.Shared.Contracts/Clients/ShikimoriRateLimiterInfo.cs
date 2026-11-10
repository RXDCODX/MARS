namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Состояние рейт-лимитера Shikimori: сколько запросов доступно и когда
/// окно сдвинется.
/// </summary>
/// <remarks>
/// Время в секундах, а не <c>TimeSpan</c>: так его не испортит сериализация
/// <c>System.Text.Json</c> в разных версиях формата.
/// </remarks>
public sealed record ShikimoriRateLimiterInfo(
    int AvailablePerSecond,
    int AvailablePerMinute,
    double SecondsToResetSecondWindow,
    double SecondsToResetMinuteWindow
);
