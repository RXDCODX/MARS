using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardMichaelTime</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class MichaelTimeHandler(
    ITelegramusNotifier notifier,
    ILogger<MichaelTimeHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardMichaelTime;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => notifier.MichaelJackson());
        }
        else
        {
            await notifier.MichaelJackson();
        }

        logger.LogInformation("MichaelTime activated by {UserName}", rewardEvent.UserName);
    }
}
