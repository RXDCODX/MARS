using MARS.Alerts.Services.Twitch.Rewards.Activation;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Лимиты награды: одна активация на зрителя за стрим и общий кулдаун.
/// </summary>
/// <remarks>
/// Проверяется порядок проверок: повтор зрителя важнее кулдауна, потому что
/// повтор должен вернуть баллы, а не ответить «подожди». Обе вещи видны
/// зрителю в чате, поэтому проверяются вместе с решением.
/// </remarks>
public class PerStreamRewardLimiterTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(20);

    [Fact]
    public void FirstActivationIsAllowed()
    {
        var limiter = Create(out _);

        var decision = limiter.TryActivate("fumofridaynight", "user-1", Cooldown);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void SameUserCannotActivateTwicePerStream()
    {
        var limiter = Create(out _);

        limiter.TryActivate("fumofridaynight", "user-1", TimeSpan.Zero);
        var repetition = limiter.TryActivate("fumofridaynight", "user-1", TimeSpan.Zero);

        Assert.Equal(RewardLimitDenial.AlreadyActivatedThisStream, repetition.Denial);
    }

    /// <summary>
    /// Второй зритель во время общего кулдауна получает отказ, но не «уже
    /// активировал»: баллы ему не возвращаются, и текст в чате другой.
    /// </summary>
    [Fact]
    public void AnotherUserDuringCooldownGetsGlobalCooldown()
    {
        var limiter = Create(out _);

        limiter.TryActivate("fumofridaynight", "user-1", Cooldown);
        var second = limiter.TryActivate("fumofridaynight", "user-2", Cooldown);

        Assert.Equal(RewardLimitDenial.GlobalCooldown, second.Denial);
        Assert.Equal(1200, second.RemainingSeconds);
    }

    [Fact]
    public void RemainingSecondsCountDown()
    {
        var limiter = Create(out var time);

        limiter.TryActivate("fumofridaynight", "user-1", Cooldown);
        time.Advance(TimeSpan.FromMinutes(5));
        var second = limiter.TryActivate("fumofridaynight", "user-2", Cooldown);

        Assert.Equal(900, second.RemainingSeconds);
    }

    [Fact]
    public void LimitsArePerReward()
    {
        var limiter = Create(out _);

        limiter.TryActivate("fumofridaynight", "user-1", TimeSpan.Zero);
        var otherReward = limiter.TryActivate("mikumonday", "user-1", TimeSpan.Zero);

        Assert.True(otherReward.IsAllowed);
    }

    [Fact]
    public void SameUserIsAllowedAfterCooldownAndNewDay()
    {
        var limiter = Create(out var time);

        limiter.TryActivate("fumofridaynight", "user-1", Cooldown);
        time.Advance(TimeSpan.FromDays(1));
        var nextDay = limiter.TryActivate("fumofridaynight", "user-1", Cooldown);

        Assert.True(nextDay.IsAllowed);
    }

    /// <summary>
    /// Новый день сбрасывает и состав зрителей, и общий кулдаун: «стрим» в
    /// монолите — это календарный день, а не жизнь процесса.
    /// </summary>
    [Fact]
    public void NewDayResetsGlobalCooldownToo()
    {
        var limiter = Create(out var time);

        limiter.TryActivate("fumofridaynight", "user-1", TimeSpan.FromHours(2));
        time.Advance(TimeSpan.FromDays(1));
        var otherUser = limiter.TryActivate("fumofridaynight", "user-2", TimeSpan.FromHours(2));

        Assert.True(otherUser.IsAllowed);
    }

    [Theory]
    [InlineData("", "user-1")]
    [InlineData("   ", "user-1")]
    [InlineData("fumofridaynight", "")]
    public void BlankKeyOrUserIsNotLimited(string alertKey, string userId)
    {
        var limiter = Create(out _);

        var first = limiter.TryActivate(alertKey, userId, Cooldown);
        var second = limiter.TryActivate(alertKey, userId, Cooldown);

        Assert.True(first.IsAllowed);
        Assert.True(second.IsAllowed);
    }

    private static PerStreamRewardLimiter Create(out RewardTestTimeProvider time)
    {
        time = new RewardTestTimeProvider(Start);

        return new PerStreamRewardLimiter(time);
    }
}
