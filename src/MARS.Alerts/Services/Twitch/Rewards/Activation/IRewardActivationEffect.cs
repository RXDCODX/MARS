namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Серверная часть награды канала: побочное действие, обогащение или лимит.
/// </summary>
/// <remarks>
/// Эффект не рисует и не знает про оверлей: он возвращает
/// <see cref="RewardEffectResult"/>, а отправляет его
/// <see cref="RewardRedemptionProcessor"/>. Регистрируются эффекты по
/// <see cref="AlertKey"/> — ключу из данных награды, а не по названию в Twitch.
/// </remarks>
public interface IRewardActivationEffect
{
    /// <summary>Ключ награды из <c>RewardAlertKeys</c>, которую обслуживает эффект.</summary>
    string AlertKey { get; }

    Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    );
}
