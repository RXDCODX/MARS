using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
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
using MARS.WaifuGacha.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._4_SearchWife;

/// <summary>
/// Обработчик reward-события <c>RewardSearchWife</c>.
/// </summary>
public class SearchWifeHandler(
    ILogger<SearchWifeHandler> logger,
    ITwitchEventValidationService validator,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    IWaifuGachaClient waifuGachaClient,
    ITwitchInternalClient twitchClient,
    IHostApplicationLifetime lifetime,
    RickRollerService rickRollerService,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 4;
    public override string AlertDisplayName { get; set; } = "🔍 Поиск супруга";
    public override string AlertDescription { get; set; } = "⠀";
    public override Color Color { get; set; } = Color.FromArgb(24, 0, 255);
    public override int Cost { get; init; } = 4;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(
                validator,
                OnRewardRedeemedAsync,
                Cost,
                Activation,
                requireFollower: true
            )
        );
        return Task.CompletedTask;
    }

    public override IRewardActivationPolicy Activation =>
        new NotDaysPolicy([DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]);

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User!;

        await rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                var result = await waifuGachaClient.RollWaifuFullAsync(user.DisplayName, ct);
                if (result?.Waifu is null)
                {
                    if (result is { IsCooldown: true, RemainingSeconds: > 0 })
                    {
                        var minutes = Math.Max(1, (result.RemainingSeconds + 59) / 60);
                        await twitchClient.SendChatMessageAsync(
                            new TwitchChatSendRequest
                            {
                                Message =
                                    $"@{rewardEvent.UserName}, кулдаун поиска супруга! Подожди ещё {minutes} мин.",
                            },
                            ct
                        );
                    }

                    logger.LogWarning("SearchWife: no waifu for {UserName}", rewardEvent.UserName);
                    return;
                }

                var waifuRoll = new WaifuRollEvent
                {
                    Waifu = new Waifu
                    {
                        ShikiId = result.Waifu.ShikiId,
                        Name = result.Waifu.Name,
                        ImageUrl = result.Waifu.ImageUrl,
                        Anime = result.Waifu.Anime ?? string.Empty,
                    },
                    Husband = result.Husband,
                };

                await hubContext.Clients.All.WaifuRoll(waifuRoll);
            }
        );

        logger.LogInformation("SearchWife activated by {UserName}", rewardEvent.UserName);
    }
}
