using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Rewards;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик одной награды. Все обработчики разбирает единственный
/// <see cref="RewardAlertConsumer"/>: отдельного AMQP-соединения на награду нет.
/// </summary>
public interface IRewardAlertHandler
{
    /// <summary>
    /// Локальный ключ награды без префикса и внешнего exchange:
    /// нормализованное название, например <c>phonkedit</c>.
    /// </summary>
    string RewardCode { get; }

    RewardDefinition Definition { get; }

    IRewardActivationPolicy Activation { get; }

    /// <summary>Обрабатывает событие редемпшна награды.</summary>
    Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct);
}
