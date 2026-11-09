using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для заключения брака. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class WaifuMarriageHandler(ILogger<WaifuMarriageHandler> logger)
    : PendingRewardAlertHandler(logger, "💒 Свадьба") { }
