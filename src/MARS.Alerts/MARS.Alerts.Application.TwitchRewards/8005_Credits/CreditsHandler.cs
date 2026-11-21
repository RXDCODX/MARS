using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards._8005_Credits;

/// <summary>
/// Обработчик reward-события <c>RewardCredits</c>.
/// </summary>
public class CreditsHandler(
    ILogger<CreditsHandler> logger,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "credits";

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
                        new TelegramusEvent { Credits = new EmptyEvent() }
                    );
                }
            );
        }

        logger.LogInformation("Credits activated by {UserName}", rewardEvent.UserName);
    }
}
