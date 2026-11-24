namespace MARS.Twitch.Infrastructure.Services.HelloVideos;

/// <summary>
/// DTO for hello video alert data.
/// </summary>
public class HelloVideoAlertDto
{
    public required string DisplayName { get; init; }
    public required string Message { get; init; }
    public string? ChatColor { get; init; }
    public Guid MediaInfoId { get; init; }
}
