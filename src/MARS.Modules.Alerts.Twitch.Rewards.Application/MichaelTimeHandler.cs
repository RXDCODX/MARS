using MARS.Platform.Grpc.Infrastructure.Notifications;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

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
    public string RewardKey => "michaeltime";

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
