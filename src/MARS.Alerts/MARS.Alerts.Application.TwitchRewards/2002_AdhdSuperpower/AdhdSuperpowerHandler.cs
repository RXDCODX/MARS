using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardAdhdSuperpower</c>.
/// </summary>
public class AdhdSuperpowerHandler(
    ILogger<AdhdSuperpowerHandler> logger,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "adhd_superpower";

    private const int AdhdDurationSeconds = 60;

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

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
                async () =>
                {
                    await _broadcaster.BroadcastAsync(
                        new TelegramusEvent
                        {
                            Adhd = new AdhdEvent { Seconds = AdhdDurationSeconds },
                        }
                    );
                }
            );
        }

        logger.LogInformation(
            "ADHD activated by {UserName} for {Duration} seconds",
            rewardEvent.UserName,
            AdhdDurationSeconds
        );
    }
}
