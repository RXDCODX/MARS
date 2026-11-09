using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для экранных эмодзи. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class EmojisHandler(ILogger<EmojisHandler> logger)
    : PendingRewardAlertHandler(logger, "😀 Эмодзи!") { }
