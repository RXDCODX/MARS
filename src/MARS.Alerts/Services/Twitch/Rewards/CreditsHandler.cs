using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardCredits</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CreditsHandler(
    ITelegramusNotifier notifier,
    ILogger<CreditsHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardCredits;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => notifier.Credits());
        }
        else
        {
            await notifier.Credits();
        }

        logger.LogInformation("Credits activated by {UserName}", rewardEvent.UserName);
    }
}
