using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardWhat</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class WhatHandler(ILogger<WhatHandler> logger) : RewardHandlerBase
{
    public string RewardKey => "what";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("WHAT reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
