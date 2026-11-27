using Grpc.Core;
using MARS.Alerts.Application;
using MARS.Alerts.Application.RewardInput;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.RewardInput;

public sealed class AllRefundRedemptionListener(
    AllRefundState state,
    IMarsGrpcClient grpcClient,
    ILogger<AllRefundRedemptionListener> logger
) : MarsEventListener<RewardRedeemedEvent>
{
    public override async Task Handle(RewardRedeemedEvent message, CancellationToken ct)
    {
        if (message is null || !state.IsActive)
        {
            return;
        }

        if (message.RewardCode == "AllRefundHandler")
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

        try
        {
            await grpcClient.Twitch.RefundRewardAsync(
                message.RewardId,
                [message.MessageId],
                "AllRefund active window"
            );
        }
        catch (RpcException)
        {
            // сервис недоступен — поведение REST-клиента: false, игнорируем
        }

        logger.LogInformation(
            "AllRefund: queued refund for {RewardCode} redemption {MessageId}",
            message.RewardCode,
            message.MessageId
        );
    }
}
