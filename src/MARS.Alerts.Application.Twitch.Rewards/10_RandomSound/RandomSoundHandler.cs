using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// заглушка для случайного звука. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class RandomSoundHandler(ILogger<RandomSoundHandler> logger)
    : PendingRewardAlertHandler(logger, "🔊 Random sound") { }
