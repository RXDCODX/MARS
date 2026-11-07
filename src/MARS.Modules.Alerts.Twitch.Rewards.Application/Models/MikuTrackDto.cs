using MARS.Shared.Contracts.Models;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application.Models;

public class MikuTrackDto
{
    public required Guid Id { get; set; }
    public int Number { get; set; }
    public required string Artist { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public string? ThumbnailUrl { get; set; }
}
