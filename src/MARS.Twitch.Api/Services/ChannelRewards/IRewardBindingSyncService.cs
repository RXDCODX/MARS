using MARS.Shared.Contracts.Rewards;

namespace MARS.Twitch.Api.Services.ChannelRewards;

public interface IRewardBindingSyncService
{
    Task<RewardBindingReport> SyncAsync(CancellationToken cancellationToken = default);
}
