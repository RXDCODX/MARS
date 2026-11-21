using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.Twitch.Rewards._4_SearchWife;

/// <summary>
/// Обработчик reward-события <c>RewardSearchWife</c>.
/// </summary>
public class SearchWifeHandler(
    ILogger<SearchWifeHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster,
    IMessageBus messageBus
) : RewardHandlerBase
{
    public override string RewardCode => "search_wife";

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
                var result = await waifuGachaClient.RollWaifuFullAsync(user.DisplayName, ct);
                if (result is null || string.IsNullOrWhiteSpace(result.WaifuJson))
                {
                    await messageBus.PublishAsync(
                        new ChatSendEvent
                        {
                            Message =
                                $"@{rewardEvent.UserName}, кулдаун поиска супруга или нет супруга в базе.",
                        }
                    );
                    logger.LogWarning("SearchWife: no waifu for {UserName}", rewardEvent.UserName);
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.WaifuJson))
                {
                    logger.LogWarning("SearchWife: no waifu for {UserName}", rewardEvent.UserName);
                    return;
                }

                var color = string.IsNullOrWhiteSpace(result.HostColor)
                    ? "#ffffff"
                    : result.HostColor;

                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        WaifuRoll = new WaifuRollEvent
                        {
                            Waifu = ParseWaifuInfo(result.WaifuJson),
                            DisplayName = result.HostDisplayName,
                            Color = color,
                        },
                    }
                );
            }
        );

        logger.LogInformation("SearchWife activated by {UserName}", rewardEvent.UserName);
    }

    private static MARS.Shared.Contracts.Telegramus.WaifuInfo ParseWaifuInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new MARS.Shared.Contracts.Telegramus.WaifuInfo
        {
            Id = root.TryGetProperty("id", out var id) ? id.ToString() : string.Empty,
            Name = root.TryGetProperty("name", out var name)
                ? name.GetString() ?? string.Empty
                : string.Empty,
            ImageUrl =
                root.TryGetProperty("imageUrl", out var img) ? img.GetString() ?? string.Empty
                : root.TryGetProperty("image_url", out var img2) ? img2.GetString() ?? string.Empty
                : string.Empty,
            Source = root.TryGetProperty("source", out var src)
                ? src.GetString() ?? string.Empty
                : string.Empty,
        };
    }
}
