using MARS.Alerts.Models;
using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда «Конфетти»: сервер передаёт вид частиц, рисует клиент.
/// </summary>
public sealed class ConfettiEffect : IRewardActivationEffect
{
    public string AlertKey => RewardAlertKeys.Confetti;

    public Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<RewardEffectResult>(
            new() { Data = new { Particles = TwitchScreenParticles.Confetty } }
        );
    }
}
