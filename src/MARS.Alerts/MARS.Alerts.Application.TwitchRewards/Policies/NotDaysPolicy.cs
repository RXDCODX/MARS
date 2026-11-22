using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards;

public sealed class NotDaysPolicy(IEnumerable<DayOfWeek> days, TimeProvider? timeProvider = null)
    : IRewardActivationPolicy
{
    private readonly HashSet<DayOfWeek> _days = days.ToHashSet();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken
    ) => ValueTask.FromResult(!_days.Contains(_timeProvider.GetUtcNow().DayOfWeek));
}
