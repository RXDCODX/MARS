using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardDanceDance</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class DanceDanceHandler(ILogger<DanceDanceHandler> logger) : RewardHandlerBase
{
    public string RewardKey => "dancedance";

    protected override async Task OnRewardRedeemedAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        logger.LogInformation("DanceDance reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
