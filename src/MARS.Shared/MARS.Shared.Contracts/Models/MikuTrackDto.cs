namespace MARS.Shared.Contracts.Models;

/// <summary>
/// Контрактная копия трека Miku Monday (владелец — MARS.Alerts).
/// </summary>
public class MikuTrackDto
{
    public required Guid Id { get; set; }
    public int Number { get; set; }
    public required string Artist { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public string? ThumbnailUrl { get; set; }
}
