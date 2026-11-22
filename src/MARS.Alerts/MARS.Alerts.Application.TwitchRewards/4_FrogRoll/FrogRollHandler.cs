using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._4_FrogRoll;

/// <summary>
/// Обработчик reward-события <c>RewardFrogRoll</c>.
/// </summary>
public class FrogRollHandler(
    ILogger<FrogRollHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "frog_roulette";
    public override int RewardCost { get; } = 4;
    public override string AlertDisplayName { get; set; } = "🐸 Frog Roulette";
    public override string AlertDescription { get; set; } = "Крути рулетку лягушек! ♪";
    public override Color Color { get; set; } = Color.FromArgb(76, 175, 80);
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

                await hubContext.Clients.All.FrogRoll(
                    new FrogRollEvent
                    {
                        FrogJson = JsonSerializer.Serialize(result.Frog),
                        TwitchUserJson = JsonSerializer.Serialize(user),
                        Color = color,
                    }
                );
            }
        );

        logger.LogInformation("FrogRoll activated by {UserName}", rewardEvent.UserName);
    }
}
