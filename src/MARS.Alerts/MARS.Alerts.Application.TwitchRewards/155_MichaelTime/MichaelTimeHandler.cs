using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards._155_MichaelTime;

/// <summary>
/// Обработчик reward-события <c>RewardMichaelTime</c>.
/// </summary>
public class MichaelTimeHandler(
    ILogger<MichaelTimeHandler> logger,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "michael_time";

    public override IRewardActivationPolicy Activation => new ServiceActivePolicy(() => true);

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
                    await broadcaster.BroadcastAsync(
                        new TelegramusEvent { MichaelJackson = new EmptyEvent() }
                    );
                }
            );
        }

        logger.LogInformation("MichaelTime activated by {UserName}", rewardEvent.UserName);
    }
}
