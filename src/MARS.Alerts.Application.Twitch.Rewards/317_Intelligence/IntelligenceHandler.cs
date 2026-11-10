using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardIntelligence</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class IntelligenceHandler(ILogger<IntelligenceHandler> logger) : RewardHandlerBase
{
    public override string RewardKey => "intelligence";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation("Intelligence reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
