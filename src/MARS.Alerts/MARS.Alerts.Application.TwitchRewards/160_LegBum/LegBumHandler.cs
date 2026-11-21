using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.TwitchRewards._160_LegBum;

/// <summary>
/// Обработчик reward-события <c>RewardLegBum</c>: возврат баллов, если ввод
/// содержит «asp/асп».
/// </summary>
public class LegBumHandler(ILogger<LegBumHandler> logger, IMessageBus messageBus)
    : RewardHandlerBase
{
    public override string RewardCode => "leg_bum";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var input = rewardEvent.UserInput ?? string.Empty;
        if (
            !input.Contains("asp", StringComparison.OrdinalIgnoreCase)
            && !input.Contains("асп", StringComparison.OrdinalIgnoreCase)
        )
        {
            logger.LogInformation(
                "LegBum: no refund keyword in input from {UserName}",
                rewardEvent.UserName
            );
            return;
        }

        var user = rewardEvent.User;
        if (
            user is null
            || string.IsNullOrWhiteSpace(rewardEvent.RewardId)
            || string.IsNullOrWhiteSpace(rewardEvent.MessageId)
        )
        {
            return;
        }

        await messageBus.PublishAsync(
            new RewardRefundEvent
            {
                RewardId = rewardEvent.RewardId,
                RedemptionIds = [rewardEvent.MessageId],
                Reason = "LegBum: asp keyword",
            }
        );

        await messageBus.PublishAsync(
            new ChatSendEvent
            {
                Message =
                    $"@{rewardEvent.UserName}, твои {rewardEvent.Cost} баллов были возвращены!",
            }
        );

        logger.LogInformation("LegBum: refunded redemption for {UserName}", rewardEvent.UserName);
    }
}
