using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

//TODO: доделать после того как остальные награды перенесут
/// <summary>
/// заглушка для рулетки лягушек. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class FrogRollHandler(ILogger<FrogRollHandler> logger)
    : PendingRewardAlertHandler(logger, "🐸 Frog Roulette") { }
