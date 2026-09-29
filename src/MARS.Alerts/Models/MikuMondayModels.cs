using MARS.Shared.Models;

namespace MARS.Alerts.Models;

public class MikuMondayDto
{
    public Guid Id { get; set; }
    public required TwitchUser TwitchUser { get; set; }
    public required MikuTrackDto SelectedTrack { get; set; }
    public required List<MikuTrackDto> AvailableTracks { get; set; }
    public bool SkipAvailableTracksUpdate { get; set; }
}

public class MikuTrackDto
{
    public required Guid Id { get; set; }
    public int Number { get; set; }
    public required string Artist { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class MikuMondayResult
{
    public MikuMondayTrackInfo? Track { get; set; }
    public List<MikuMondayTrackInfo> AvailableTracks { get; set; } = [];
    public string? Error { get; set; }
    public bool IsSuccess => Track != null && string.IsNullOrWhiteSpace(Error);
}

public class MikuMondayTrackInfo
{
    public int Id { get; set; }
    public int Number { get; set; }
    public Guid BaseTrackInfoId { get; set; }
    public string? TrackName { get; set; }
    public string? Artist { get; set; }
    public string? Url { get; set; }
    public string? ArtworkUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class MikuMondayActivationInfo
{
    public int Id { get; set; }
    public required string TwitchUserId { get; set; }
    public required string DisplayName { get; set; }
    public int MikuMondayTrackId { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.Now;
    public int WeekOfYear { get; set; }
    public int Year { get; set; }
}

public class MikuTrackJson
{
    public int Number { get; set; }
    public required string Artist { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
}
