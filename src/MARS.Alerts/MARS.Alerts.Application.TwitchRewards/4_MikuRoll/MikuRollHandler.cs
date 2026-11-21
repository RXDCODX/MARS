using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.Overlay;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.WaifuGacha.Models.Rest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._4_MikuRoll;

/// <summary>
/// Обработчик reward-события <c>RewardMikuRoll</c>.
/// </summary>
public class MikuRollHandler(
    ILogger<MikuRollHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    IOverlayEventSink broadcaster,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "miku_roulette";
    public override int RewardCost { get; } = 4;
    public override string AlertDisplayName { get; set; } = "🎀 Мику Рулетка";
    public override string AlertDescription { get; set; } =
        "Крути рулетку костюмов Miku по понедельникам! ♪";
    public override Color Color { get; set; } = Color.FromArgb(57, 197, 187);
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
                var result = await waifuGachaClient.RollMikuFullAsync(user.TwitchId, ct);
                if (result is null || result.Module is null)
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
                            MikuModuleJson = JsonSerializer.Serialize(result.Module),
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
