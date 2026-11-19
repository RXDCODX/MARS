using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

//TODO: доделать после того как остальные награды перенесут
/// <summary>
/// заглушка для аудиовикторины. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class AudioQuizHandler(ILogger<AudioQuizHandler> logger)
    : PendingRewardAlertHandler(logger, "🎵 Аудио викторина") { }
