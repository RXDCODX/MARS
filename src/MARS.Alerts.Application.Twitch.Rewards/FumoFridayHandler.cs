using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для регистрации и активации Fumo Friday. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class FumoFridayHandler(ILogger<FumoFridayHandler> logger)
    : PendingRewardAlertHandler(logger, "🌙 Fumo Friday") { }
