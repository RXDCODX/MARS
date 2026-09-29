namespace MARS.TwitchCore.Services.YouTube;

/// <summary>
/// Базовая информация о треке для YouTube-резолвера.
/// </summary>
public class BaseTrackInfo
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string TrackName { get; init; }
    public string[]? Authors { get; init; }
    public TimeSpan Duration { get; init; }
    public required Uri Url { get; init; }
    public Uri? ArtworkUrl { get; init; }
    public string? VideoId { get; init; }

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
