using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Policies;

public sealed class CooldownPolicy(TimeSpan cooldown, TimeProvider? timeProvider = null)
    : IRewardActivationPolicy
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Lock _gate = new();
    private DateTimeOffset? _lastActivated;

    public bool IsStateful => true;

    public ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken
    )
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            if (_lastActivated is null || now - _lastActivated.Value >= cooldown)
            {
                _lastActivated = now;
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }
    }
}
