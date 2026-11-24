using System.Drawing;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services.ChannelRewards;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.Rewards;

/// <summary>
/// Interface for reward cost identification.
/// </summary>
public interface ITwitchReward
{
    public int Cost { get; init; }
}
