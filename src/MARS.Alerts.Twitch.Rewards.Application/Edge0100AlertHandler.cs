using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardEdge0100Alert</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class Edge0100AlertHandler(ILogger<Edge0100AlertHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "edge0100alert";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("Edge0100Alert reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
