using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик одного reward-routing key.
/// Все обработчики разбирает единственный <see cref="RewardAlertConsumer"/>, поэтому
/// на каждую награду больше не поднимается отдельная BackgroundService с собственным
/// AMQP-соединением, каналом и собственной копией топологии exchange.
/// </summary>
public interface IRewardAlertHandler
{
    /// <summary>Routing key награды, которую обрабатывает хендлер.</summary>
    string RoutingKey { get; }

    /// <summary>Обрабатывает событие редемпшна награды.</summary>
    Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct);
}
