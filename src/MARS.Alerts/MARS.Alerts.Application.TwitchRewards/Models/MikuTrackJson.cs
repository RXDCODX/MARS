using MARS.Shared.Contracts.Models;
using MARS.Shared.Kernel;

namespace MARS.Alerts.Application.TwitchRewards.Models;

public class MikuTrackJson
{
    public int Number { get; set; }
    public required string Artist { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
}
