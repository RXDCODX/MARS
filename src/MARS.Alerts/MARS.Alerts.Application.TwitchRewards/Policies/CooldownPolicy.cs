using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards;

public sealed class CooldownPolicy(TimeSpan cooldown, TimeProvider? timeProvider = null)
    : IRewardActivationPolicy
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly object _gate = new();
    private DateTimeOffset? _lastActivated;

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
