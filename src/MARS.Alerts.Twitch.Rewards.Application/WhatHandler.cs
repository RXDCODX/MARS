using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardWhat</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class WhatHandler(ILogger<WhatHandler> logger) : IRewardAlertHandler
{
    public string RewardKey => "what";

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("WHAT reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
