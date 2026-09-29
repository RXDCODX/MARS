using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace MARS.TwitchCore.Services.ChannelRewards;

public class RewardsCacheService(
    Func<Task<IEnumerable<CustomReward>?>> getRewardsAsync,
    ILogger logger
) : IRewardsCacheService
{
    private readonly Func<Task<IEnumerable<CustomReward>?>> _getRewardsAsync =
        getRewardsAsync ?? throw new ArgumentNullException(nameof(getRewardsAsync));
    private readonly SemaphoreSlim _semaphore = new(1);
    private ImmutableList<CustomReward>? _cachedRewards;
    private DateTime? _cacheExpirationTime;
    private const int CacheTtlMinutes = 2;

    public async Task<IEnumerable<CustomReward>?> GetRewardsAsync()
    {
        await _semaphore.WaitAsync();

        if (IsCacheValid() && _cachedRewards != null)
        {
            logger.LogDebug("Использование кешированных наград ({Count} шт)", _cachedRewards.Count);
            _semaphore.Release();
            return _cachedRewards.AsEnumerable();
        }

        _semaphore.Release();

        try
        {
            var rewards = await _getRewardsAsync();
            rewards = rewards?.ToArray();
            if (rewards is not null)
            {
                await _semaphore.WaitAsync();
                _cachedRewards = rewards.ToImmutableList();
                _cacheExpirationTime = DateTime.Now.AddMinutes(CacheTtlMinutes);
                _semaphore.Release();

                logger.LogInformation(
                    "Кеш наград обновлён ({Count} шт, TTL: {Minutes} мин)",
                    _cachedRewards.Count,
                    CacheTtlMinutes
                );

                return rewards;
            }

            logger.LogWarning("GetRewardsAsync вернул null");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Исключение при получении наград");
            return null;
        }
    }

    public async Task InvalidateCacheAsync()
    {
        await _semaphore.WaitAsync();
        _cachedRewards = null;
        _cacheExpirationTime = null;
        _semaphore.Release();
        logger.LogInformation("Кеш наград инвалидирован");
    }

    public CacheInfo GetCacheInfo()
    {
        _semaphore.Wait();
        var isCached = IsCacheValid();
        _semaphore.Release();

        return new CacheInfo(
            IsCached: isCached,
            CachedAt: _cacheExpirationTime?.AddMinutes(-CacheTtlMinutes),
            CachedRewardsCount: _cachedRewards?.Count,
            TimeToExpire: _cacheExpirationTime.HasValue
                ? _cacheExpirationTime.Value - DateTime.Now
                : null
        );
    }

    private bool IsCacheValid()
    {
        return _cachedRewards != null
            && _cacheExpirationTime.HasValue
            && DateTime.Now < _cacheExpirationTime.Value;
    }
}
