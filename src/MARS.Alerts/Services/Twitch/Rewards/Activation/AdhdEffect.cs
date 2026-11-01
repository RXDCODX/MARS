using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда ADHD Superpower: длительность экранного эффекта задаёт сервер.
/// </summary>
public sealed class AdhdEffect : IRewardActivationEffect
{
    /// <summary>Столько же, сколько было в монолите и в прежнем обработчике.</summary>
    private const int AdhdDurationSeconds = 60;

    public string AlertKey => RewardAlertKeys.AdhdSuperpower;

    public Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<RewardEffectResult>(
            new() { Data = new { Seconds = AdhdDurationSeconds } }
        );
    }
}
