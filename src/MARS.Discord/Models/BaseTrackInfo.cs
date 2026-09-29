namespace MARS.Discord.Models;

public class BaseTrackInfo
{
    public Guid Id { get; set; }
    public required string TrackName { get; set; }
    public string[]? Authors { get; set; }
    public TimeSpan Duration { get; set; }
    public required Uri Url { get; init; }
    public Uri? ArtworkUrl { get; set; }
    public string? VideoId { get; set; }

    public string Title
    {
        get
        {
            if (Authors is { Length: > 0 })
            {
                var authors = string.Join(',', Authors);
                return string.Concat(authors, ' ', '-', ' ', TrackName);
            }

            return TrackName;
        }
    }
}
