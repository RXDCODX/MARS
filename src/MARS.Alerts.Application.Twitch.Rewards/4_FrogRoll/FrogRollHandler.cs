using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для рулетки лягушек. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class FrogRollHandler(ILogger<FrogRollHandler> logger)
    : PendingRewardAlertHandler(logger, "🐸 Frog Roulette") { }
