using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardWednsdayFrog</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class WednsdayFrogHandler(ILogger<WednsdayFrogHandler> logger) : RewardHandlerBase
{
    public override string RewardCode => "wednesday_frog";

    public override IRewardActivationPolicy Activation => new DayOfWeekPolicy(DayOfWeek.Wednesday);

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation("WednsdayFrog reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
