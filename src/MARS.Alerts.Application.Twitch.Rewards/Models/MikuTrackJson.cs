using MARS.Shared.Contracts.Models;

namespace MARS.Alerts.Application.Twitch.Rewards.Models;

public class MikuTrackJson
{
    public int Number { get; set; }
    public required string Artist { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
}
