using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardSkibidibop</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class SkibidibopHandler(ILogger<SkibidibopHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "skibidibop";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("Skibidibop reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
