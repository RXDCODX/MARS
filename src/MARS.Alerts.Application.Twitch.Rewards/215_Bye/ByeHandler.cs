using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardBye</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class ByeHandler(ILogger<ByeHandler> logger) : RewardHandlerBase
{
    public override string RewardKey => "bye";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation("Bye reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
