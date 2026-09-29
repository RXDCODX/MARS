using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardWhat</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class WhatHandler(ILogger<WhatHandler> logger) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardWhat;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("WHAT reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
