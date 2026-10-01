namespace MARS.TwitchCore.Services.HelloVideos;

/// <summary>
/// Interface for sending hello video alerts.
/// Abstracts the delivery of hello video alerts to the OBS source.
/// </summary>
public interface IHelloVideoNotifier
{
    Task SendAlertAsync(HelloVideoAlertDto alert);
}

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
