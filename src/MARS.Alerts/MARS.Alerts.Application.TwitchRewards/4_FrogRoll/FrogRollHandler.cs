using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.WaifuGacha.Models.Rest;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._4_FrogRoll;

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
                if (result is null || result.Frog is null)
                {
                    logger.LogWarning(
                        "FrogRoll: no frog returned for {UserName}",
                        rewardEvent.UserName
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(user.ChatColor) ? "#ffffff" : user.ChatColor;

                await broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        FrogRoll = new FrogRollEvent
                        {
                            FrogJson = JsonSerializer.Serialize(result.Frog),
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
