using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardFumoFridayNight</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class FumoFridayNightHandler(
    ILogger<FumoFridayNightHandler> logger,
    RickRollerService rickRollerService
) : RewardHandlerBase
{
    public override string RewardKey => "fumofridaynight";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => Task.CompletedTask);
        }
        else
        {
            // await notifier.FumoFriday(rewardEvent.UserName, null);
        }

        logger.LogInformation("FumoFridayNight activated by {UserName}", rewardEvent.UserName);
    }
}
