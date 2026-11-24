using System.Drawing;
using MARS.Alerts.Application.TwitchRewards._2002_AdhdSuperpower;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._2002_AdhdSuperpower;

/// <summary>
/// Обработчик reward-события <c>RewardAdhdSuperpower</c>.
/// </summary>
public class AdhdSuperpowerHandler(
    ILogger<AdhdSuperpowerHandler> logger,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 2002;
    public override string AlertDisplayName { get; set; } = "⚡ ADHD Superpower";
    public override string AlertDescription { get; set; } = "🔥 Активируй ADHD режим на 60 секунд!";
    public override Color Color { get; set; } = Color.FromArgb(138, 43, 226);
    public override int Cost { get; init; } = 2002;

    public override Type? CommandPayloadType => typeof(AdhdAlertCommandPayload);

    public override async Task<string> InvokeFromCommandAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        if (payload is not AdhdAlertCommandPayload adhd)
        {
            return "Неверный тип параметров для adhd.";
        }

        var seconds = adhd.Seconds <= 0 ? 0 : adhd.Seconds;
        try
        {
            await hubContext.Clients.All.Adhd(new AdhdEvent { Seconds = seconds });

            return seconds > 0
                ? $"✅ ADHD эффект активирован на {seconds} секунд!"
                : "✅ ADHD эффект переключён в перманентный режим!";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка при активации ADHD эффекта: {ex.Message}";
        }
    }

    private const int AdhdDurationSeconds = 60;

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

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                async () =>
                {
                    await hubContext.Clients.All.Adhd(
                        new AdhdEvent { Seconds = AdhdDurationSeconds }
                    );
                }
            );
        }

        logger.LogInformation(
            "ADHD activated by {UserName} for {Duration} seconds",
            rewardEvent.UserName,
            AdhdDurationSeconds
        );
    }
}
