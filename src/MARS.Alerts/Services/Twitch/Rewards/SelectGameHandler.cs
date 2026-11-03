using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardSelectGame</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class SelectGameHandler(ILogger<SelectGameHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardSelectGame;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation(
            "SelectGame reward redeemed by {UserName} with input: {Input}",
            rewardEvent.UserName,
            rewardEvent.UserInput
        );
    }
}
