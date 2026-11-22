using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts.Clients;

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
