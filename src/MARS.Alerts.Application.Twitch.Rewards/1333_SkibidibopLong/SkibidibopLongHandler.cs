using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardSkibidibopLong</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class SkibidibopLongHandler(ILogger<SkibidibopLongHandler> logger) : RewardHandlerBase
{
    public override string RewardKey => "skibidiboplong";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation("SkibidibopLong reward redeemed by {UserName}", rewardEvent.UserName);
    }
}
