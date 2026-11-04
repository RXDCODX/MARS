using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

public sealed class RewardRefundRequestor(IMarsEventBus eventBus) : IRewardRefundRequestor
{
    public Task RequestAsync(
        RewardRefundEvent request,
        CancellationToken cancellationToken = default
    )
    {
        return eventBus.PublishAsync(
            RabbitMqConfig.RoutingKeys.RewardRefund,
            request,
            cancellationToken
        );
    }
}
