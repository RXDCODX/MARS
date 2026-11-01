using MARS.Alerts.Models;
using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда «Фейерверк»: сервер передаёт вид частиц, рисует клиент.
/// </summary>
public sealed class FireworksEffect : IRewardActivationEffect
{
    public string AlertKey => RewardAlertKeys.Fireworks;

    public Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<RewardEffectResult>(
            new() { Data = new { Particles = TwitchScreenParticles.Fireworks } }
        );
    }
}
