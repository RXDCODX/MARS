using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardLegBum</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class LegBumHandler(ILogger<LegBumHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "legbum";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation(
            "LegBum reward redeemed by {UserName} with input: {Input}",
            rewardEvent.UserName,
            rewardEvent.UserInput
        );
    }
}
