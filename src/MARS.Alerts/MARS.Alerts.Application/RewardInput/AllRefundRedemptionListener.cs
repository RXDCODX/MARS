using MARS.Alerts.Application.RewardInput;
using MARS.Alerts.Application;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.RewardInput;

public sealed class AllRefundRedemptionListener(
    AllRefundState state,
    IMessageBus messageBus,
    ILogger<AllRefundRedemptionListener> logger
) : MarsEventListener<RewardRedeemedEvent>
{
    public override async Task Handle(RewardRedeemedEvent message, CancellationToken ct)
    {
        if (message is null || !state.IsActive)
        {
            return;
        }

        if (message.RewardCode == "all_refund")
        {
            return;
        }

        if (
            string.IsNullOrWhiteSpace(message.RewardId)
            || string.IsNullOrWhiteSpace(message.MessageId)
        )
        {
            return;
        }

        await messageBus.PublishAsync(
            new RewardRefundEvent
            {
                RewardId = message.RewardId,
                RedemptionIds = [message.MessageId],
                Reason = "AllRefund active window",
            }
        );

        logger.LogInformation(
            "AllRefund: queued refund for {RewardCode} redemption {MessageId}",
            message.RewardCode,
            message.MessageId
        );
    }
}