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

namespace MARS.Alerts.Application.Twitch.Rewards._5_AddWife;

/// <summary>
/// Обработчик reward-события <c>RewardAddWife</c>.
/// </summary>
public class AddWifeHandler(
    ILogger<AddWifeHandler> logger,
    IWaifuGachaClient waifuGachaClient,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster,
    IMessageBus messageBus
) : RewardHandlerBase
{
    public override string RewardCode => "add_wife";

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

        var input = rewardEvent.UserInput ?? string.Empty;

        await rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                var result = await waifuGachaClient.AddNewWaifuFullAsync(
                    input,
                    user.TwitchId,
                    user.DisplayName,
                    user.IsVip,
                    ct
                );

                if (result is null || string.IsNullOrWhiteSpace(result.WaifuJson))
                {
                    await messageBus.PublishAsync(
                        new ChatSendEvent { Message = "Не удалось добавить супруга! :-(" }
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(result.Color) ? "#ffffff" : result.Color;

                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        AddNewWaifu = new AddNewWaifuEvent
                        {
                            Waifu = ParseWaifuInfo(result.WaifuJson),
                            DisplayName = result.DisplayName,
                            Color = color,
                        },
                    }
                );

                if (result.VipDropped)
                {
                    await messageBus.PublishAsync(
                        new ChatAnnouncementEvent
                        {
                            Message =
                                $"@{user.DisplayName}, новый супруг {result.DisplayName} добавлен! Осталось до гаранта: {result.RollsUntilGuarantee} роллов!",
                        }
                    );
                    await messageBus.PublishAsync(
                        new TwitchVipGrantEvent { UserId = user.TwitchId }
                    );
                }
                else
                {
                    await messageBus.PublishAsync(
                        new ChatSendEvent
                        {
                            Message =
                                $"@{user.DisplayName}, новый супруг {result.DisplayName} добавлен! Осталось до гаранта: {result.RollsUntilGuarantee} роллов!",
                        }
                    );
                }
            }
        );

        logger.LogInformation("AddWife activated by {UserName}", rewardEvent.UserName);
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
}

