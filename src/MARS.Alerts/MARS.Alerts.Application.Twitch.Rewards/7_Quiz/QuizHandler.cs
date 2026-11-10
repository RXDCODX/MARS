using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для текстовой викторины. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class QuizHandler(ILogger<QuizHandler> logger)
    : PendingRewardAlertHandler(logger, "🧠 Викторина") { }
