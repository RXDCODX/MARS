using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда «Выключить игру»: закрывает процессы игры и ничего не рисует.
/// </summary>
public sealed class CloseGameEffect(IGameCloser gameCloser, ILogger<CloseGameEffect> logger)
    : IRewardActivationEffect
{
    public string AlertKey => RewardAlertKeys.CloseGame;

    public async Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        await gameCloser.CloseAsync(cancellationToken);

        logger.LogInformation("CloseGame activated by {UserName}", context.UserName);

        return RewardEffectResult.Suppressed;
    }
}
