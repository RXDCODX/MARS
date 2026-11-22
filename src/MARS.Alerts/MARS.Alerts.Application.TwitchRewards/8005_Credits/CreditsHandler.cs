using System.Drawing;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Models.Overlay;
using Microsoft.AspNetCore.SignalR;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._8005_Credits;

/// <summary>
/// Обработчик reward-события <c>RewardCredits</c>.
/// </summary>
public class CreditsHandler(
    ILogger<CreditsHandler> logger,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "credits";
    public override int RewardCost { get; } = 8005;
    public override string AlertDisplayName { get; set; } = "🎬 Credits";
    public override string AlertDescription { get; set; } = "🎞️ Показать кредиты на экране";
    public override Color Color { get; set; } = Color.FromArgb(255, 215, 0);
    public override int Cost { get; init; } = 8005;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

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
                    await hubContext.Clients.All.Credits();
                }
            );
        }

        logger.LogInformation("Credits activated by {UserName}", rewardEvent.UserName);
    }
}