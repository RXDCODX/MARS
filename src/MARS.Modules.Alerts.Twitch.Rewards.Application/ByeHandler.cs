using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardBye</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class ByeHandler(ILogger<ByeHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "bye";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("Bye reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
