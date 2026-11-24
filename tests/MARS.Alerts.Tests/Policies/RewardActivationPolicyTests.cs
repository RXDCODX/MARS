using System;
using System.Threading;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Shared.Contracts.Messaging;
using Xunit;

namespace MARS.Alerts.Tests.Policies;

public class RewardActivationPolicyTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTimeOffset value) => _utcNow = value;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    private static RewardRedeemedEvent AnyEvent() => new() { Cost = 1 };

    [Theory]
    [InlineData(DayOfWeek.Monday, DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Monday, DayOfWeek.Tuesday, false)]
    public async Task DayOfWeekPolicy_MatchesConfiguredDay(
        DayOfWeek configured,
        DayOfWeek now,
        bool expected
    )
    {
        var baseMonday = new DateTime(2026, 1, 5); // Monday
        var target = baseMonday.AddDays(((int)now - (int)DayOfWeek.Monday + 7) % 7);
        var tp = new FixedTimeProvider(new DateTimeOffset(target, TimeSpan.Zero));

        var policy = new DayOfWeekPolicy(configured, tp);
        var result = await policy.IsActiveAsync(AnyEvent(), CancellationToken.None);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task NotDaysPolicy_BlocksListedDays()
    {
        var tp = new FixedTimeProvider(new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero)); // Monday
        var policy = new NotDaysPolicy([DayOfWeek.Monday, DayOfWeek.Wednesday], tp);
        Assert.False(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));

        tp.SetUtcNow(new DateTimeOffset(2026, 1, 6, 12, 0, 0, TimeSpan.Zero)); // Tuesday
        Assert.True(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));
    }

    [Fact]
    public async Task DisabledPolicy_AlwaysFalse()
    {
        var policy = DisabledPolicy.Instance;
        Assert.False(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));
    }

    [Fact]
    public async Task AlwaysActivePolicy_AlwaysTrue()
    {
        Assert.True(
            await AlwaysActiveRewardPolicy.Instance.IsActiveAsync(
                AnyEvent(),
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task CompositePolicy_RequiresAllChildren()
    {
        var monday = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);
        var tp = new FixedTimeProvider(monday);
        var composite = new CompositeRewardPolicy(
            new DayOfWeekPolicy(DayOfWeek.Monday, tp),
            new NotDaysPolicy([DayOfWeek.Friday], tp)
        );
        Assert.True(await composite.IsActiveAsync(AnyEvent(), CancellationToken.None));

        tp.SetUtcNow(new DateTimeOffset(2026, 1, 9, 12, 0, 0, TimeSpan.Zero)); // Friday
        var composite2 = new CompositeRewardPolicy(
            new DayOfWeekPolicy(DayOfWeek.Monday, tp),
            new NotDaysPolicy([DayOfWeek.Friday], tp)
        );
        Assert.False(await composite2.IsActiveAsync(AnyEvent(), CancellationToken.None));
    }

    [Fact]
    public async Task CooldownPolicy_FirstCallActivatesThenBlocks()
    {
        var tp = new FixedTimeProvider(new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero));
        var policy = new CooldownPolicy(TimeSpan.FromMinutes(3), tp);
        Assert.True(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));
        Assert.False(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));

        tp.Advance(TimeSpan.FromMinutes(4));
        Assert.True(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));
    }

    [Fact]
    public async Task ServiceActivePolicy_DelegatesToFunc()
    {
        var flag = true;
        var policy = new ServiceActivePolicy(() => flag);
        Assert.True(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));
        flag = false;
        Assert.False(await policy.IsActiveAsync(AnyEvent(), CancellationToken.None));
    }

    [Fact]
    public void CooldownPolicy_IsStateful()
    {
        var policy = new CooldownPolicy(TimeSpan.FromMinutes(1));
        Assert.True(((IRewardActivationPolicy)policy).IsStateful);
    }

    [Fact]
    public void DayOfWeekPolicy_IsNotStateful()
    {
        var policy = new DayOfWeekPolicy(DayOfWeek.Monday);
        Assert.False(((IRewardActivationPolicy)policy).IsStateful);
    }
}
