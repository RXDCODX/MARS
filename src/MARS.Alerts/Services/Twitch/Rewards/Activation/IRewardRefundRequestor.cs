using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Просьба вернуть баллы за редемпшн.
/// </summary>
/// <remarks>
/// Возврат требует токена стримера (<c>UpdateRedemptionStatus</c>), а он есть
/// только у MARS.TwitchCore. MARS.Alerts публикует
/// <see cref="RabbitMqConfig.RoutingKeys.RewardRefund"/>, а не пытается вернуть баллы сам,
/// иначе второй клиент Twitch API разошёлся бы с владельцем токена.
/// </remarks>
public interface IRewardRefundRequestor
{
    Task RequestAsync(RewardRefundEvent request, CancellationToken cancellationToken = default);
}
