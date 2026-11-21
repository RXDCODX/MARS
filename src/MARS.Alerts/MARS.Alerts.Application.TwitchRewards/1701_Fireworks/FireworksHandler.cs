using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;

namespace MARS.Alerts.Application.Twitch.Rewards._1701_Fireworks;

/// <summary>
/// Обработчик reward-события <c>RewardFireworks</c>.
/// </summary>
public class FireworksHandler(
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "fireworks";

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

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
                    await _broadcaster.BroadcastAsync(
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

