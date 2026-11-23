using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
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
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager,
    ITwitchInternalClient twitchClient
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 4;
    public override string AlertDisplayName { get; set; } = "🎀 Мику Рулетка";
    public override string AlertDescription { get; set; } =
        "Крути рулетку костюмов Miku по понедельникам! ♪";
    public override Color Color { get; set; } = Color.FromArgb(57, 197, 187);
    public override int Cost { get; init; } = 4;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override IRewardActivationPolicy Activation => new DayOfWeekPolicy(DayOfWeek.Monday);

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
                if (result?.Module is null)
                {
                    if (result is { IsCooldown: true, RemainingSeconds: > 0 })
                    {
                        var minutes = Math.Max(1, (result.RemainingSeconds + 59) / 60);
                        await twitchClient.SendChatMessageAsync(
                            new TwitchChatSendRequest
                            {
                                Message =
                                    $"@{rewardEvent.UserName}, кулдаун на MikuRoll! Подожди ещё {minutes} мин.",
                            },
                            ct
                        );
                    }

                    logger.LogWarning(
                        "MikuRoll: no miku module returned for {UserName}",
                        rewardEvent.UserName
                    );
                    return;
                }

                await hubContext.Clients.All.MikuRoll(
                    new MikuRollEvent
                    {
                        Module = result.Module,
                        User = user,
                        CollectedCount = result.CollectedCount,
                        TotalCount = result.TotalCount,
                    }
                );
            }
        );

        logger.LogInformation("MikuRoll activated by {UserName}", rewardEvent.UserName);
    }
}
