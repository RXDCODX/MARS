using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для рулетки фумо по пятницам. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class FumoFridayRollHandler(ILogger<FumoFridayRollHandler> logger)
    : PendingRewardAlertHandler(logger, "🧸 Fumo Roulette") { }
