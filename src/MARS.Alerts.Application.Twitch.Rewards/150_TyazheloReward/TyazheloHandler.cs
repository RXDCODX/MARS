using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardTyazhelo</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class TyazheloHandler(ILogger<TyazheloHandler> logger) : RewardHandlerBase
{
    public override string RewardKey => "tyazhelo";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation("Tyazhelo reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
