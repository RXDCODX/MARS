using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Models.Overlay;
using Microsoft.AspNetCore.SignalR;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.WaifuGacha.Models.Rest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.TwitchRewards._2_WaifuMarriage;

/// <summary>
/// Обработчик reward-события <c>RewardWaifuMarriage</c>.
/// </summary>
public class WaifuMarriageHandler(
    ILogger<WaifuMarriageHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    IMessageBus messageBus,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "waifu_marriage";
    public override int RewardCost { get; } = 2;
    public override string AlertDisplayName { get; set; } = "💒 Свадьба";
    public override string AlertDescription { get; set; } =
        "💝 Свадьба с твоим будущим супругом раз и навсегда!";
    public override Color Color { get; set; } = Color.FromArgb(0, 18, 255);
    public override int Cost { get; init; } = 2;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override IRewardActivationPolicy Activation =>
        new NotDaysPolicy(new[] { DayOfWeek.Friday, DayOfWeek.Wednesday, DayOfWeek.Monday });

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
                var result = await waifuGachaClient.MergeWaifuFullAsync(user.TwitchId, ct);
                if (result is null)
                {
                    await messageBus.PublishAsync(
                        new ChatSendEvent
                        {
                            Message = $"@{rewardEvent.UserName}, ты новенький, тебе пока нельзя!",
                        }
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(result.Color) ? "#ffffff" : result.Color;

                if (result.IsNewMarriage)
                {
                    var waifuInfo = result.Waifu is null
                        ? new WaifuInfo()
                        : new WaifuInfo
                        {
                            Id = result.Waifu.ShikiId,
                            Name = result.Waifu.Name,
                            ImageUrl = result.Waifu.ImageUrl,
                            Source = result.Waifu.Anime ?? string.Empty,
                        };
                    var husbandInfo = result.Husband is null
                        ? new HusbandInfo()
                        : new HusbandInfo
                        {
                            Id = result.Husband.TwitchId,
                            DisplayName = result.Husband.TwitchId,
                            AvatarUrl = string.Empty,
                        };

                    await hubContext.Clients.All.MergeWaifu(new MergeWaifuEvent
                            {
                                Waifu = waifuInfo,
                                Husband = husbandInfo,
                                Avatar = result.Avatar,
                                Color = color,
                            });
                }
                else
                {
                    var waifuInfo = result.Waifu is null
                        ? new WaifuInfo()
                        : new WaifuInfo
                        {
                            Id = result.Waifu.ShikiId,
                            Name = result.Waifu.Name,
                            ImageUrl = result.Waifu.ImageUrl,
                            Source = result.Waifu.Anime ?? string.Empty,
                        };
                    var husbandInfo = result.Husband is null
                        ? new HusbandInfo()
                        : new HusbandInfo
                        {
                            Id = result.Husband.TwitchId,
                            DisplayName = result.Husband.TwitchId,
                            AvatarUrl = string.Empty,
                        };

                    await hubContext.Clients.All.ShowCurrentWife(new ShowCurrentWifeEvent
                            {
                                Waifu = waifuInfo,
                                Husband = husbandInfo,
                                Avatar = result.Avatar,
                                Color = color,
                            });
                }
            });
        logger.LogInformation("WaifuMarriage activated by {UserName}", rewardEvent.UserName);
    }
}