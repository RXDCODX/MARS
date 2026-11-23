using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Policies;

public sealed class DayOfWeekPolicy(DayOfWeek day, TimeProvider? timeProvider = null)
    : IRewardActivationPolicy
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken
    ) => ValueTask.FromResult(_timeProvider.GetUtcNow().DayOfWeek == day);
}
