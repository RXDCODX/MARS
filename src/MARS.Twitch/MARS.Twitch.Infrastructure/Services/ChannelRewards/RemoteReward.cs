using MARS.Shared.Contracts.Rewards;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public sealed record RemoteReward(string Id, string Title, int Cost);
