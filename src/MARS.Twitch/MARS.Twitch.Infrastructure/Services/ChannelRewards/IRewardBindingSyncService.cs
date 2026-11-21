using MARS.Shared.Contracts.Rewards;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public interface IRewardBindingSyncService
{
    Task<RewardBindingReport> SyncAsync(CancellationToken cancellationToken = default);
}
