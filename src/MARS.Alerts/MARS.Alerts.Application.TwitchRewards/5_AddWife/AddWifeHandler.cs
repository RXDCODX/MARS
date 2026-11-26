using System.Drawing;
using Grpc.Core;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Kernel;
using MARS.WaifuGacha.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._5_AddWife;

/// <summary>
/// Обработчик reward-события <c>RewardAddWife</c>.
/// </summary>
public class AddWifeHandler(
    ILogger<AddWifeHandler> logger,
    IMarsGrpcClient grpcClient,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 5;
    public override string AlertDisplayName { get; set; } = "👰 Добавить супруга";
    public override string AlertDescription { get; set; } =
        "✨ Есть шанс получить VIP если выбить число >= 95!";
    public override Color Color { get; set; } = Color.FromArgb(0, 30, 255);
    public override int Cost { get; init; } = 5;

    public override IRewardActivationPolicy Activation =>
        new NotDaysPolicy([DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]);

    public override bool IsUserInputRequired { get; } = true;

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

        var input = rewardEvent.UserInput ?? string.Empty;

        await rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                MARS.Shared.Contracts.WaifuGacha.Rest.AddNewWaifuResultDto? result = null;
                try
                {
                    result = await grpcClient.WaifuGacha.AddNewWaifuFullAsync(
                        input,
                        user.TwitchId,
                        user.DisplayName,
                        user.IsVip,
                        ct
                    );
                }
                catch (RpcException)
                {
                    result = null;
                }

                if (result?.Waifu is null)
                {
                    await grpcClient.Twitch.SendChatMessageAsync(
                        new MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest
                        {
                            Message = "Не удалось добавить супруга! :-(",
                        }
                    );
                    return;
                }

                var color = string.IsNullOrWhiteSpace(result.Color) ? "#ffffff" : result.Color;

                var waifuInfo = new Waifu
                {
                    ShikiId = result.Waifu.ShikiId,
                    Name = result.Waifu.Name,
                    ImageUrl = result.Waifu.ImageUrl,
                    Anime = result.Waifu.Anime ?? string.Empty,
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
                    await grpcClient.Twitch.SendChatMessageAsync(
                        new MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest
                        {
                            Message =
                                $"@{user.DisplayName}, новый супруг {result.DisplayName} добавлен! Осталось до гаранта: {result.RollsUntilGuarantee} роллов!",
                        }
                    );
                    await grpcClient.Twitch.GrantVipAsync(user.TwitchId);
                }
                else
                {
                    await grpcClient.Twitch.SendChatMessageAsync(
                        new MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest
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
