using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardFumoFridayNight</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class FumoFridayNightHandler(
    ITelegramusNotifier notifier,
    ILogger<FumoFridayNightHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardFumoFridayNight;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => notifier.FumoFriday(rewardEvent.UserName, user.ChatColor)
            );
        }
        else
        {
            await notifier.FumoFriday(rewardEvent.UserName, null);
        }

        logger.LogInformation("FumoFridayNight activated by {UserName}", rewardEvent.UserName);
    }
}
