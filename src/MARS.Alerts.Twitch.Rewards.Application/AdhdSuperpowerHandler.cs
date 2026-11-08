using MARS.Platform.Grpc.Infrastructure.Notifications;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Обработчик reward-события <c>RewardAdhdSuperpower</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class AdhdSuperpowerHandler(
    ITelegramusNotifier notifier,
    ILogger<AdhdSuperpowerHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RewardKey => "adhdsuperpower";

    private const int AdhdDurationSeconds = 60;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => notifier.Adhd(AdhdDurationSeconds)
            );
        }
        else
        {
            await notifier.Adhd(AdhdDurationSeconds);
        }

        logger.LogInformation(
            "ADHD activated by {UserName} for {Duration} seconds",
            rewardEvent.UserName,
            AdhdDurationSeconds
        );
    }
}
