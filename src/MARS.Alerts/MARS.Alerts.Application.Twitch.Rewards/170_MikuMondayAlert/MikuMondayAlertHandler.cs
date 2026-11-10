using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для Miku Monday Alert. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class MikuMondayAlertHandler(ILogger<MikuMondayAlertHandler> logger)
    : PendingRewardAlertHandler(logger, "🎤 Miku Monday Alert") { }
