using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards._4_FrogRoll;

/// <summary>
/// Обработчик reward-события <c>RewardFrogRoll</c>.
/// </summary>
public class FrogRollHandler(
    ILogger<FrogRollHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "frog_roulette";

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is null)
        {
            return;
        }

        await rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                var result = await waifuGachaClient.RollFrogFullAsync(ct);
                if (result is null || string.IsNullOrWhiteSpace(result.FrogJson))
                {
                    logger.LogWarning(
                        "FrogRoll: no frog returned for {UserName}",
                        rewardEvent.UserName
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(user.ChatColor) ? "#ffffff" : user.ChatColor;

                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        FrogRoll = new FrogRollEvent
                        {
                            FrogJson = result.FrogJson,
                            TwitchUserJson = JsonSerializer.Serialize(user),
                            Color = color,
                        },
                    }
                );
            }
        );

        logger.LogInformation("FrogRoll activated by {UserName}", rewardEvent.UserName);
    }
}

