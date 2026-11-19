using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

//TODO: доделать после того как остальные награды перенесут
/// <summary>
/// заглушка для рулетки Мику. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class MikuRollHandler(ILogger<MikuRollHandler> logger)
    : PendingRewardAlertHandler(logger, "🎀 Мику Рулетка") { }
