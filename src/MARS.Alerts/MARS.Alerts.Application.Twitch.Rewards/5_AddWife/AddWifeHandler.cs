using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

//TODO: доделать после того как остальные награды перенесут
/// <summary>
/// заглушка для добавления супруга. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class AddWifeHandler(ILogger<AddWifeHandler> logger)
    : PendingRewardAlertHandler(logger, "👰 Добавить супруга") { }
