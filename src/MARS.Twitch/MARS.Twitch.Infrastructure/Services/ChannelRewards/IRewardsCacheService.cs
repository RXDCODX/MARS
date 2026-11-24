using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public interface IRewardsCacheService
{
    Task<IEnumerable<CustomReward>?> GetRewardsAsync();
    Task InvalidateCacheAsync();
    CacheInfo GetCacheInfo();
}
