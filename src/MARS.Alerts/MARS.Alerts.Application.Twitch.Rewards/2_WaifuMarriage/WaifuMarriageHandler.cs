using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

//TODO: доделать после того как остальные награды перенесут
/// <summary>
/// заглушка для заключения брака. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class WaifuMarriageHandler(ILogger<WaifuMarriageHandler> logger)
    : PendingRewardAlertHandler(logger, "💒 Свадьба") { }
