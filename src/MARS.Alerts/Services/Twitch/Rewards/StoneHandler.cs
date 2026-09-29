using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardStone</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class StoneHandler(ILogger<StoneHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardStone;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("Stone reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
