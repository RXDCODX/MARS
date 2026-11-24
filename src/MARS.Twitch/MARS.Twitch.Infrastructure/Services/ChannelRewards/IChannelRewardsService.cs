using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

/// <summary>
/// Награды канала — Helix CRUD + кеш.
/// </summary>
public interface IChannelRewardsService
{
    IRewardsCacheService RewardsCacheService { get; }

    Task<IEnumerable<CustomReward>?> GetRewardsAsync();

    Task<string?> CreateRewardAsync(CreateCustomRewardsRequest request);

    Task<bool> UpdateRewardAsync(string rewardId, UpdateCustomRewardRequest request);

    Task<bool> DeleteRewardAsync(string rewardId);

    Task<CustomReward?> GetRewardByIdAsync(string rewardId);
}
