using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// DTO трека Miku Monday, возвращаемый сервисом <c>MARS.WaifuGacha</c>.
/// </summary>
public sealed record MikuTrackDto(
    Guid Id,
    int Number,
    string Artist,
    string Title,
    string Url,
    string? ThumbnailUrl
);

/// <summary>
/// Результат выбора трека Miku Monday.
/// </summary>
public sealed class MikuMondayResult
{
    public MikuTrackDto? Track { get; set; }
    public List<MikuTrackDto> AvailableTracks { get; set; } = [];
    public string? Error { get; set; }
    public bool IsSuccess => Track is not null && string.IsNullOrWhiteSpace(Error);
}

/// <summary>
/// Данные для отправки фронтенду (SignalR-алерт Miku Monday).
/// </summary>
public sealed class MikuMondayDto
{
    public Guid Id { get; set; }
    public required TwitchUser TwitchUser { get; set; }
    public required MikuTrackDto SelectedTrack { get; set; }
    public required List<MikuTrackDto> AvailableTracks { get; set; }
    public bool SkipAvailableTracksUpdate { get; set; }
}
