using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
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

namespace MARS.Alerts.Application.TwitchRewards._2_WaifuMarriage;

/// <summary>
/// Обработчик rewad-события <c>RewardWaifuMarriage</c>.
/// </summary>
public class WaifuMarriageHandler(
    ILogger<WaifuMarriageHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchInternalClient twitchClient,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 2;
    public override string AlertDisplayName { get; set; } = "💒 Свадьба";
    public override string AlertDescription { get; set; } =
        "💝 Свадьба с твоим будущим супругом раз и навсегда!";
    public override Color Color { get; set; } = Color.FromArgb(0, 18, 255);
    public override int Cost { get; init; } = 2;

    public override IRewardActivationPolicy Activation =>
        new NotDaysPolicy([DayOfWeek.Friday, DayOfWeek.Wednesday, DayOfWeek.Monday]);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, Activation)
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
                var result = await waifuGachaClient.MergeWaifuFullAsync(user.TwitchId, ct);
                if (result is null)
                {
                    await twitchClient.SendChatMessageAsync(
                        new TwitchChatSendRequest
                        {
                            Message = $"@{rewardEvent.UserName}, ты новенький, тебе пока нельзя!",
                        }
                    );
                    return;
                }

                if (result.IsNewMarriage)
                {
                    var waifuInfo = result.Waifu is null
                        ? new Waifu
                        {
                            ShikiId = "",
                            Name = "",
                            ImageUrl = "",
                        }
                        : new Waifu
                        {
                            ShikiId = result.Waifu.ShikiId,
                            Name = result.Waifu.Name,
                            ImageUrl = result.Waifu.ImageUrl,
                            Anime = result.Waifu.Anime ?? string.Empty,
                        };
                    await hubContext.Clients.All.MergeWaifu(
                        new MergeWaifuEvent { Waifu = waifuInfo, Husband = result.Husband }
                    );
                }
                else
                {
                    var waifuInfo = result.Waifu is null
                        ? new Waifu
                        {
                            ShikiId = "",
                            Name = "",
                            ImageUrl = "",
                        }
                        : new Waifu
                        {
                            ShikiId = result.Waifu.ShikiId,
                            Name = result.Waifu.Name,
                            ImageUrl = result.Waifu.ImageUrl,
                            Anime = result.Waifu.Anime ?? string.Empty,
                        };

                    await hubContext.Clients.All.ShowCurrentWife(
                        new ShowCurrentWifeEvent { Waifu = waifuInfo, Husband = result.Husband }
                    );
                }
            }
        );
        logger.LogInformation("WaifuMarriage activated by {UserName}", rewardEvent.UserName);
    }
}
