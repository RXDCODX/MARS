using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Models.Overlay;
using Microsoft.AspNetCore.SignalR;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Models;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._18_GaoAlert;

/// <summary>
/// Обработчик reward-события <c>RewardGaoAlert</c>.
/// </summary>
public class GaoAlertHandler(
    ILogger<GaoAlertHandler> logger,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "gao_alert";
    public override int RewardCost { get; } = 18;
    public override string AlertDisplayName { get; set; } = "🦁 GAO Alert";
    public override string AlertDescription { get; set; } = "";
    public override Color Color { get; set; } = Color.FromArgb(255, 255, 0);
    public override int Cost { get; init; } = 18;
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
        var text = (rewardEvent.UserInput ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("GaoAlert: empty input from {UserName}", rewardEvent.UserName);
            return;
        }

        var normalized = text.TrimStart('@');

        if (!IsValidTwitchUsername(normalized))
        {
            var dto = new GaoAlertDto { IsJustText = true, JustText = normalized };
            await PublishAsync(dto, rewardEvent);
            return;
        }

        var candidate = normalized;
        var dto2 = new GaoAlertDto
        {
            TwitchUser = new { DisplayName = candidate },
            IsJustText = false,
        };
        await PublishAsync(dto2, rewardEvent);
    }

    private async Task PublishAsync(GaoAlertDto dto, RewardRedeemedEvent rewardEvent)
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
                await hubContext.Clients.All.GaoAlert(new GaoAlertEvent
                        {
                            GaoAlertJson = JsonSerializer.Serialize(dto),
                        });
            }
        );
    }

    private static bool IsValidTwitchUsername(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 25
        && value.All(c => char.IsLetterOrDigit(c) || c == '_');
}