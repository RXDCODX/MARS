using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public sealed class PyroAlertRewardDefinition : ChannelRewardDefinition
{
    public Guid? MediaInfoId { get; init; }
}
