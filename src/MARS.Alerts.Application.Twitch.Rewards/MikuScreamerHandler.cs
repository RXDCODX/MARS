using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardMikuScreamer</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class MikuScreamerHandler(ILogger<MikuScreamerHandler> logger) : RewardHandlerBase
{
    public string RewardKey => "mikuscreamer";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("MikuScreamer reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
