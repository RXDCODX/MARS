using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardWednsdayFrog</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class WednsdayFrogHandler(ILogger<WednsdayFrogHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "wednsdayfrog";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("WednsdayFrog reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
