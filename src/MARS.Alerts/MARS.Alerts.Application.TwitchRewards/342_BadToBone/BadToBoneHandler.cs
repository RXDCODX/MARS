using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardBadToBone</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class BadToBoneHandler(ILogger<BadToBoneHandler> logger) : RewardHandlerBase
{
    public override string RewardCode => "bad_to_bone";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "BadToBone reward redeemed by {UserName} with input: {Input}",
            rewardEvent.UserName,
            rewardEvent.UserInput
        );
    }
}
