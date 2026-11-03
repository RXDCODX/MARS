using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardDanceDance</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class DanceDanceHandler(ILogger<DanceDanceHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardDanceDance;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("DanceDance reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
