using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
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
                        new TelegramusEvent { MichaelJackson = new EmptyEvent() }
                    );
                }
            );
        }

        logger.LogInformation("MichaelTime activated by {UserName}", rewardEvent.UserName);
    }
}

