namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>Решение по лимитам награды.</summary>
/// <remarks>
/// <see cref="RemainingSeconds"/> заполняется только для
/// <see cref="RewardLimitDenial.GlobalCooldown"/>: зрителю показывают, сколько
/// ждать, и число секунд входит в текст сообщения в чат.
/// </remarks>
public readonly record struct RewardLimitDecision(
    RewardLimitDenial Denial,
    int RemainingSeconds = 0
)
{
    public bool IsAllowed => Denial == RewardLimitDenial.None;
}
