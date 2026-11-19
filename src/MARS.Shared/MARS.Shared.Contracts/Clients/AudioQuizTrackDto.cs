namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Трек для аудиовикторины: URL, обложка, название.
/// </summary>
public sealed record AudioQuizTrackDto(
    string TrackUrl,
    string? ArtworkUrl,
    string Title,
    int DurationSeconds
);
