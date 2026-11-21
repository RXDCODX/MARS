using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.Overlay;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Application.TwitchRewards._1700_Confetti;

/// <summary>
/// Обработчик reward-события <c>RewardConfetti</c>.
/// </summary>
public class ConfettiHandler(
    RickRollerService rickRollerService,
    IOverlayEventSink broadcaster,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "confetti";
    public override int RewardCost { get; } = 1700;
    public override string AlertDisplayName { get; set; } = "🎉 Конфетти!";
    public override string AlertDescription { get; set; } = "";
    public override Color Color { get; set; } = Color.FromArgb(0, 255, 47);
    public override int Cost { get; init; } = 1700;
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
                    await broadcaster.BroadcastAsync(
                        new TelegramusEvent
                        {
                            MakeScreenParticles = new ScreenParticlesEvent
                            {
                                ParticlesJson = JsonSerializer.Serialize("Confetty"),
                            },
                        }
                    );
                }
            );
        }
    }
}
