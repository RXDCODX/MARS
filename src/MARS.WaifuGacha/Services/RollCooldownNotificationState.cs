using MARS.WaifuGacha.Entities;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Общее состояние уведомлений о завершении кулдауна ролла.
/// Разделяется между двумя листенерами (<see cref="RollCooldownRewardListener"/>
/// и <see cref="RollCooldownChatListener"/>), так как каждое сообщение шины
/// обрабатывается своим лисенером.
/// </summary>
public sealed class RollCooldownNotificationState
{
    private readonly Dictionary<(string UserId, string RollType), DateTime> _pendingNotifications =
        new();
    private readonly HashSet<(string UserId, string RollType)> _notifiedUsers = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<(DateTime CooldownEnd, bool Added)> AddPendingAsync(
        string userId,
        string rollType,
        TimeSpan cooldown,
        CancellationToken ct
    )
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var key = (userId, rollType);
            var cooldownEnd = DateTime.UtcNow + cooldown;

            _pendingNotifications[key] = cooldownEnd;
            _notifiedUsers.Remove(key);

            return (cooldownEnd, true);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<(string UserId, string RollType)>> GetExpiredAsync(
        string userId,
        CancellationToken ct
    )
    {
        var expired = new List<(string UserId, string RollType)>();

        await _semaphore.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;

            var userKeys = _pendingNotifications
                .Keys.Where(key => string.Equals(key.UserId, userId, StringComparison.Ordinal))
                .ToList();

            foreach (var key in userKeys)
            {
                var cooldownEnd = _pendingNotifications[key];

                if (now >= cooldownEnd)
                {
                    _pendingNotifications.Remove(key);
                    expired.Add(key);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return expired;
    }

    public async Task<bool> MarkNotifiedAsync(string userId, string rollType, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (_notifiedUsers.Count > 1000)
            {
                _notifiedUsers.Clear();
            }

            // Возвращает true, если пользователь уже был уведомлен (запись была в
            // наборе до этого вызова), и false — если только что добавлен.
            return !_notifiedUsers.Add((userId, rollType));
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
