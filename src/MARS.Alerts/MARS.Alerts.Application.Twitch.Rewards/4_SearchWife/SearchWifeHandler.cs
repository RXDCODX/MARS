using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

//TODO: доделать после того как остальные награды перенесут
/// <summary>
/// заглушка для поиска супруга. Пока зарегистрирована только маршрутизация; реализация будет добавлена отдельно.
/// </summary>
public sealed class SearchWifeHandler(ILogger<SearchWifeHandler> logger)
    : PendingRewardAlertHandler(logger, "🔍 Поиск супруга") { }
