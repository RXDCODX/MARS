using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.WaifuGacha.Models.Rest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._4_FumoRoll;

/// <summary>
/// Обработчик reward-события <c>RewardFumoRoll</c>.
/// </summary>
public class FumoFridayRollHandler(
    ILogger<FumoFridayRollHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "fumo_roulette";
    public override int RewardCost { get; } = 4;
    public override string AlertDisplayName { get; set; } = "🧸 Fumo Roulette";
    public override string AlertDescription { get; set; } = "Крути рулетку Fumo по пятницам! ♪";
    public override Color Color { get; set; } = Color.FromArgb(255, 182, 193);
    public override int Cost { get; init; } = 4;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled)
        );
        return Task.CompletedTask;
    }

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
                var result = await waifuGachaClient.RollFumoFullAsync(user.TwitchId, ct);
                if (result is null || result.Fumo is null)
                {
                    logger.LogWarning(
                        "FumoRoll: no fumo returned for {UserName}",
                        rewardEvent.UserName
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(user.ChatColor) ? "#ffffff" : user.ChatColor;

                await hubContext.Clients.All.FumoRoll(new FumoRollEvent
                        {
                            FumoJson = JsonSerializer.Serialize(result.Fumo),
                            TwitchUserJson = JsonSerializer.Serialize(user),
                            Color = color,
                            CollectedCount = result.CollectedCount,
                            TotalCount = result.TotalCount,
                        });
            }
        );

        logger.LogInformation("FumoRoll activated by {UserName}", rewardEvent.UserName);
    }
}