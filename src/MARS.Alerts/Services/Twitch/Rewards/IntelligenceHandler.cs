using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardIntelligence</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class IntelligenceHandler(ILogger<IntelligenceHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardIntelligence;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("Intelligence reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
