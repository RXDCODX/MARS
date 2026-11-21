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

namespace MARS.Alerts.Application.TwitchRewards._5_AddWife;

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

                if (result is null || result.Waifu is null)
                {
                    await messageBus.PublishAsync(
                        new ChatSendEvent { Message = "Не удалось добавить супруга! :-(" }
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(result.Color) ? "#ffffff" : result.Color;

                var waifuInfo = new WaifuInfo
                {
                    Id = result.Waifu.ShikiId,
                    Name = result.Waifu.Name,
                    ImageUrl = result.Waifu.ImageUrl,
                    Source = result.Waifu.Anime ?? string.Empty,
                };

                await hubContext.Clients.All.AddNewWaifu(
                    new AddNewWaifuEvent
                    {
                        Waifu = waifuInfo,
                        DisplayName = result.DisplayName,
                        Color = color,
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

}
