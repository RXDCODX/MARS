using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardEdge0100Alert</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class Edge0100AlertHandler(ILogger<Edge0100AlertHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardEdge0100Alert;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("Edge0100Alert reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
