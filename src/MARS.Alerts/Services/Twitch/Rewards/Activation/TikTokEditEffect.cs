using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда «Сделай TikTok-эдит»: текст зрителя и идентификатор эдита едут в
/// данных активации.
/// </summary>
/// <remarks>
/// Идентификатор создаётся на сервере, а не на клиенте: по нему экран отличает
/// новый эдит от повторного события, и два браузера OBS не должны получить
/// разные значения.
/// </remarks>
public sealed class TikTokEditEffect : IRewardActivationEffect
{
    public string AlertKey => RewardAlertKeys.TikTokEdit;

    public Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<RewardEffectResult>(
            new() { Data = new { Guid = Guid.NewGuid(), Text = context.UserInput ?? string.Empty } }
        );
    }
}
