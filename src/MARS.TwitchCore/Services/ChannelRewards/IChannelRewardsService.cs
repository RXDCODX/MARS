using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.TwitchCore.Services.ChannelRewards;

/// <summary>
/// Награды канала — контракт для <see cref="Entities.TemporaryReward"/>.
///
/// <see cref="ChannelRewardsService"/> уходит в Twitch через конкретный
/// <c>Helix</c>, у которого нет интерфейса, и подменить его в тестах нечем:
/// подставной <c>IHttpCallHandler</c> возвращает <c>null</c> молча. Поэтому
/// класс, который сам принимает решения о состоянии награды, работает с
/// интерфейсом — иначе его проверка была бы проверкой сети.
/// </summary>
public interface IChannelRewardsService
{
    IRewardsCacheService RewardsCacheService { get; }

    Task<IEnumerable<CustomReward>?> GetRewardsAsync();

    Task<string?> CreateRewardAsync(CreateCustomRewardsRequest request);

    Task<bool> UpdateRewardAsync(string rewardId, UpdateCustomRewardRequest request);

    bool? GetEnabledOverrideForCost(int cost);
}
