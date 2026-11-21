using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.WaifuGacha.Models.Rest;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
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
    EventBroadcaster<TelegramusEvent> broadcaster,
    IMessageBus messageBus
) : RewardHandlerBase
{
    public override string RewardCode => "waifu_marriage";

    public override IRewardActivationPolicy Activation =>
        new NotDaysPolicy(new[] { DayOfWeek.Friday, DayOfWeek.Wednesday, DayOfWeek.Monday });

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

                    await hubContext.Clients.All.MergeWaifu(
                        new MergeWaifuEvent
                        {
                            Waifu = waifuInfo,
                            Husband = husbandInfo,
                            Avatar = result.Avatar,
                            Color = color,
                        }
                    );
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

                    await hubContext.Clients.All.ShowCurrentWife(
                        new ShowCurrentWifeEvent
                        {
                            Waifu = waifuInfo,
                            Husband = husbandInfo,
                            Avatar = result.Avatar,
                            Color = color,
                        }
                    );
                }
            }
        );

        logger.LogInformation("WaifuMarriage activated by {UserName}", rewardEvent.UserName);
}
}
