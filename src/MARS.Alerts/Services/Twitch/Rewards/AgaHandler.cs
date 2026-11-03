using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardAga</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class AgaHandler(ILogger<AgaHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardAga;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation(
            "AGA reward redeemed by {UserName} with input: {Input}",
            rewardEvent.UserName,
            rewardEvent.UserInput
        );
    }
}
