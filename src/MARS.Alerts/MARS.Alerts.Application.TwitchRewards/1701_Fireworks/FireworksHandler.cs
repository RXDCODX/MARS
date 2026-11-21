using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards._1701_Fireworks;

/// <summary>
/// Обработчик reward-события <c>RewardFireworks</c>.
/// </summary>
public class FireworksHandler(
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "fireworks";

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
                                ParticlesJson = JsonSerializer.Serialize("Fireworks"),
                            },
                        }
                    );
                }
            );
        }
    }
}
