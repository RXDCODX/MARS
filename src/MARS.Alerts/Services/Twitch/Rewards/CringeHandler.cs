using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardCringe</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CringeHandler(ILogger<CringeHandler> logger) : IRewardAlertHandler
{
    public string RewardHandlerKey => "cringe";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("CRINGE reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
