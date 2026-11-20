using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.Twitch.Rewards._2_WaifuMarriage;

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

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

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
                    var waifuInfo = ParseWaifuInfo(result.WaifuJson);
                    var husbandInfo = ParseHusbandInfo(result.HusbandJson);

                    await _broadcaster.BroadcastAsync(
                        new TelegramusEvent
                        {
                            MergeWaifu = new MergeWaifuEvent
                            {
                                Waifu = waifuInfo,
                                Husband = husbandInfo,
                                Avatar = result.Avatar,
                                Color = color,
                            },
                        }
                    );
                }
                else
                {
                    var waifuInfo = ParseWaifuInfo(result.WaifuJson);
                    var husbandInfo = ParseHusbandInfo(result.HusbandJson);

                    await _broadcaster.BroadcastAsync(
                        new TelegramusEvent
                        {
                            ShowCurrentWife = new ShowCurrentWifeEvent
                            {
                                Waifu = waifuInfo,
                                Husband = husbandInfo,
                                Avatar = result.Avatar,
                                Color = color,
                            },
                        }
                    );
                }
            }
        );

        logger.LogInformation("WaifuMarriage activated by {UserName}", rewardEvent.UserName);
    }

    private static MARS.Shared.Contracts.Telegramus.WaifuInfo ParseWaifuInfo(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new MARS.Shared.Contracts.Telegramus.WaifuInfo();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new MARS.Shared.Contracts.Telegramus.WaifuInfo
        {
            Id =
                root.TryGetProperty("shikiId", out var id) ? id.ToString()
                : root.TryGetProperty("id", out var id2) ? id2.ToString()
                : string.Empty,
            Name = root.TryGetProperty("name", out var n)
                ? n.GetString() ?? string.Empty
                : string.Empty,
            ImageUrl =
                root.TryGetProperty("imageUrl", out var i) ? i.GetString() ?? string.Empty
                : root.TryGetProperty("image_url", out var i2) ? i2.GetString() ?? string.Empty
                : string.Empty,
            Source = root.TryGetProperty("source", out var s)
                ? s.GetString() ?? string.Empty
                : string.Empty,
        };
    }

    private static MARS.Shared.Contracts.Telegramus.HusbandInfo ParseHusbandInfo(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new MARS.Shared.Contracts.Telegramus.HusbandInfo();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new MARS.Shared.Contracts.Telegramus.HusbandInfo
        {
            Id =
                root.TryGetProperty("twitchId", out var id) ? id.ToString()
                : root.TryGetProperty("id", out var id2) ? id2.ToString()
                : string.Empty,
            DisplayName =
                root.TryGetProperty("displayName", out var d) ? d.GetString() ?? string.Empty
                : root.TryGetProperty("display_name", out var d2) ? d2.GetString() ?? string.Empty
                : string.Empty,
            AvatarUrl =
                root.TryGetProperty("avatarUrl", out var a) ? a.GetString() ?? string.Empty
                : root.TryGetProperty("avatar_url", out var a2) ? a2.GetString() ?? string.Empty
                : string.Empty,
        };
    }
}
