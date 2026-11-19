using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts.Clients;

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
