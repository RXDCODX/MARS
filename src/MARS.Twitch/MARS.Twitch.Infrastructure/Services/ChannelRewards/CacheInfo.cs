using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public record CacheInfo(
    bool IsCached,
    DateTime? CachedAt,
    int? CachedRewardsCount,
    TimeSpan? TimeToExpire
);
