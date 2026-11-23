using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._99999_AllRefundService;

/// <summary>
/// Обработчик reward-события <c>RewardAllRefund</c>.
/// </summary>
public class AllRefundHandler(
    ILogger<AllRefundHandler> logger,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchInternalClient twitchClient,
    AllRefundState state,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 99999;
    public override string AlertDisplayName { get; set; } = "💣 Алармагеддон 💣";
    public override string AlertDescription { get; set; } =
        "После активации все использованные награды возвращают баллы";
    public override Color Color { get; set; } = Color.FromArgb(128, 0, 0);
    public override int Cost { get; init; } = 99999;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    private static readonly TimeSpan ActivationDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromMinutes(1);

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

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                async () =>
                {
                    await hubContext.Clients.All.AllRefund(new AllRefundEvent { User = user });

                    await twitchClient.SendChatMessageAsync(
                        new TwitchChatSendRequest
                        {
                            Message =
                                $"{user.DisplayName} активировал Алармагеддон! Начинайте тратить баллы через 10 секунд!",
                        }
                    );

                    await Task.Delay(ActivationDelay, ct);

                    state.Activate(ActiveWindow);

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(ActiveWindow);
                        state.Deactivate();
                        await twitchClient.SendChatMessageAsync(
                            new TwitchChatSendRequest
                            {
                                Message = "Алармагеддон закончился! Всем спасибо!",
                            }
                        );
                    });
                }
            );
        }

        logger.LogInformation("AllRefund activated by {UserName}", rewardEvent.UserName);
    }
}
