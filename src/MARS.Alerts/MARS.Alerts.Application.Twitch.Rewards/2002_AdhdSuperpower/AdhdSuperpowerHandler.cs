using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardAdhdSuperpower</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class AdhdSuperpowerHandler(
    ITelegramusNotifier notifier,
    ILogger<AdhdSuperpowerHandler> logger,
    RickRollerService rickRollerService
) : RewardHandlerBase
{
    public override string RewardKey => "adhdsuperpower";

    private const int AdhdDurationSeconds = 60;

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
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
