using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace MARS.TwitchCore.Services.ChannelRewards;

public interface IRewardsCacheService
{
    Task<IEnumerable<CustomReward>?> GetRewardsAsync();
    Task InvalidateCacheAsync();
    CacheInfo GetCacheInfo();
}

public record CacheInfo(
    bool IsCached,
    DateTime? CachedAt,
    int? CachedRewardsCount,
    TimeSpan? TimeToExpire
);
