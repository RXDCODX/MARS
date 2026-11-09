using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для русской рулетки. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class RussianRouletteHandler(ILogger<RussianRouletteHandler> logger)
    : PendingRewardAlertHandler(logger, "🔫 Русская рулетка") { }
