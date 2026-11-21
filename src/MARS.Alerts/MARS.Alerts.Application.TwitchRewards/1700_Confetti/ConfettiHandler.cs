using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;

namespace MARS.Alerts.Application.Twitch.Rewards._1700_Confetti;

/// <summary>
/// Обработчик reward-события <c>RewardConfetti</c>.
/// </summary>
public class ConfettiHandler(
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "confetti";

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
