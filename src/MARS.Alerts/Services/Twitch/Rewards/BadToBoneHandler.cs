using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardBadToBone</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class BadToBoneHandler(ILogger<BadToBoneHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardBadToBone;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation(
            "BadToBone reward redeemed by {UserName} with input: {Input}",
            rewardEvent.UserName,
            rewardEvent.UserInput
        );
    }
}
