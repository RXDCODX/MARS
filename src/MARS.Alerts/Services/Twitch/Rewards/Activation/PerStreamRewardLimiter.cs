namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Лимиты «один раз за стрим на зрителя» и общий кулдаун награды.
/// </summary>
/// <remarks>
/// <para>
/// Логика перенесена из монолита один в один: сначала проверяется, не
/// активировал ли зритель награду сегодня, и только потом — общий кулдаун.
/// Порядок важен: повторный клик во время кулдауна должен вернуть баллы, а не
/// ответить «подожди N секунд».
/// </para>
/// <para>
/// «Стрим» здесь — календарный день на машине стримера, как в монолите
/// (<c>ResetIfNewDay</c>). Перезапуск сервиса сбрасывает лимиты: состояние
/// живёт в памяти, а не в базе, потому что награда — развлечение на вечер, и
/// переживать рестарт ей незачем.
/// </para>
/// </remarks>
public sealed class PerStreamRewardLimiter(TimeProvider timeProvider)
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, RewardLimitState> _states = new(StringComparer.Ordinal);

    /// <summary>
    /// Проверяет лимиты и, если они не нарушены, засчитывает активацию.
    /// </summary>
    public RewardLimitDecision TryActivate(string alertKey, string userId, TimeSpan globalCooldown)
    {
        var decision = new RewardLimitDecision(RewardLimitDenial.None);

        if (!string.IsNullOrWhiteSpace(alertKey) && !string.IsNullOrWhiteSpace(userId))
        {
            lock (_lock)
            {
                var now = timeProvider.GetLocalNow();
                var today = DateOnly.FromDateTime(now.DateTime);

                if (!_states.TryGetValue(alertKey, out var state) || state.Day != today)
                {
                    state = new RewardLimitState(today);
                    _states[alertKey] = state;
                }

                if (state.ActivatedUserIds.Contains(userId))
                {
                    decision = new RewardLimitDecision(
                        RewardLimitDenial.AlreadyActivatedThisStream
                    );
                }
                else
                {
                    var elapsed = now - state.LastActivation;

                    if (state.LastActivation != DateTimeOffset.MinValue && elapsed < globalCooldown)
                    {
                        var remaining = (int)(globalCooldown - elapsed).TotalSeconds;
                        decision = new RewardLimitDecision(
                            RewardLimitDenial.GlobalCooldown,
                            remaining > 0 ? remaining : 0
                        );
                    }
                    else
                    {
                        state.LastActivation = now;
                        state.ActivatedUserIds.Add(userId);
                    }
                }
            }
        }

        return decision;
    }

    private sealed class RewardLimitState(DateOnly day)
    {
        public DateOnly Day { get; } = day;

        public HashSet<string> ActivatedUserIds { get; } = new(StringComparer.Ordinal);

        public DateTimeOffset LastActivation { get; set; } = DateTimeOffset.MinValue;
    }
}
