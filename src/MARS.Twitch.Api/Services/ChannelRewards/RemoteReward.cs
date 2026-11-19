using MARS.Shared.Contracts.Rewards;

namespace MARS.Twitch.Api.Services.ChannelRewards;

public sealed record RemoteReward(string Id, string Title, int Cost);
