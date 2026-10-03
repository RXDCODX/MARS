namespace MARS.SoundRequest.Hubs.Dtos;

/// <summary>Трек в форме REST-контракта клиента.</summary>
/// <remarks>
/// Повторяет <c>BaseTrackInfo</c> из контракта клиента, а не proto-сообщение:
/// длительность приходит строкой <c>hh:mm:ss</c>, а не целыми секундами.
/// </remarks>
public class TrackInfoHubDto
{
    public string Id { get; set; } = string.Empty;

    public string TrackName { get; set; } = string.Empty;

    public string[]? Authors { get; set; }

    /// <summary>Длительность в формате <c>hh:mm:ss</c>.</summary>
    public string? Duration { get; set; }

    public string? Url { get; set; }

    public string? ArtworkUrl { get; set; }

    public string? VideoId { get; set; }
}
