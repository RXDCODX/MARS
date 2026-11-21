using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Interfaces;
using Wolverine;

namespace MARS.Twitch.Infrastructure.Services.Rewards;

/// <summary>
/// Потребитель <see cref="RewardRefundEvent"/>: возвращает баллы канальной награды
/// (статус CANCELED) через единственное Helix-подключение MARS.Twitch.Api.
/// </summary>
public sealed class TwitchRefundListener(
    ITwitchAPI api,
    TokenService tokenService,
    ILogger<TwitchRefundListener> logger
) : MarsEventListener<RewardRefundEvent>
{
    public override async Task Handle(RewardRefundEvent message, CancellationToken ct)
    {
        if (
            message is null
            || string.IsNullOrWhiteSpace(message.RewardId)
            || message.RedemptionIds.Count == 0
        )
        {
            logger.LogWarning("Reward refund has no reward/redemption ids, skipped");
            return;
        }

        var accessToken = tokenService.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("Reward refund: no access token available");
            return;
        }

        try
        {
            await api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                TwitchConstants.ChannelId,
                message.RewardId,
                message.RedemptionIds,
                new UpdateCustomRewardRedemptionStatusRequest
                {
                    Status = TwitchLib.Api.Core.Enums.CustomRewardRedemptionStatus.CANCELED,
                },
                accessToken
            );

            logger.LogInformation(
                "Refunded {Count} redemptions of reward {RewardId}: {Reason}",
                message.RedemptionIds.Count,
                message.RewardId,
                message.Reason
            );
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }
}
