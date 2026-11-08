using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardMikuScreamer</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class MikuScreamerHandler(ILogger<MikuScreamerHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "mikuscreamer";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("MikuScreamer reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
