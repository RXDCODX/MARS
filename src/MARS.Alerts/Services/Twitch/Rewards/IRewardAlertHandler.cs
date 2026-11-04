using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик одной награды. Все обработчики разбирает единственный
/// <see cref="RewardAlertConsumer"/>, поэтому на каждую награду больше не
/// поднимается отдельная BackgroundService с собственным AMQP-соединением,
/// каналом и собственной копией топологии exchange.
/// </summary>
public interface IRewardAlertHandler
{
    /// <summary>
    /// Внутренний идентификатор обработчика. Используется <see cref="RewardAlertConsumer"/>
    /// для диспетчеризации погашений: все награды публикуются с одним транспортным
    /// ключом <c>twitch.reward.redeemed</c>, поэтому выбор обработчика делается по
    /// нормализованному названию награды из события, а не по RabbitMQ routing key.
    /// Стабильный идентификатор позволяет переименовывать награду в Twitch без
    /// поломки диспетчеризации.
    /// </summary>
    string RewardHandlerKey { get; }

    /// <summary>Обрабатывает событие редемпшна награды.</summary>
    Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct);
}
