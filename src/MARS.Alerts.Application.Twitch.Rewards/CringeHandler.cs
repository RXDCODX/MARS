using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardCringe</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CringeHandler(ILogger<CringeHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "cringe";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("CRINGE reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
