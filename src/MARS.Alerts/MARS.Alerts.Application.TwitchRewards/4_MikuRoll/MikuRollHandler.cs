using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards._4_MikuRoll;

/// <summary>
/// Обработчик reward-события <c>RewardMikuRoll</c>.
/// </summary>
public class MikuRollHandler(
    ILogger<MikuRollHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "miku_roulette";

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
                var result = await waifuGachaClient.RollMikuFullAsync(user.TwitchId, ct);
                if (result is null || string.IsNullOrWhiteSpace(result.MikuJson))
                {
                    logger.LogWarning(
                        "MikuRoll: no miku module returned for {UserName}",
                        rewardEvent.UserName
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(user.ChatColor) ? "#ffffff" : user.ChatColor;

                await broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        MikuRoll = new MikuRollEvent
                        {
                            MikuModuleJson = result.MikuJson,
                            TwitchUserJson = JsonSerializer.Serialize(user),
                            Color = color,
                            CollectedCount = result.CollectedCount,
                            TotalCount = result.TotalCount,
                        },
                    }
                );
            }
        );

        logger.LogInformation("MikuRoll activated by {UserName}", rewardEvent.UserName);
    }
}
