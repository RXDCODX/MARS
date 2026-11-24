using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public interface ITwitchReward
{
    int Cost { get; init; }
}
