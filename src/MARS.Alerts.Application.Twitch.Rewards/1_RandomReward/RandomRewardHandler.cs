using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для случайного выбора другой награды. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class RandomRewardHandler(ILogger<RandomRewardHandler> logger)
    : PendingRewardAlertHandler(logger, "🎲 Случайная награда!") { }
