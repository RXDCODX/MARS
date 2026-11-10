using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardHello</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class HelloHandler(ILogger<HelloHandler> logger) : RewardHandlerBase
{
    public override string RewardKey => "hello";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation("Hello reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
