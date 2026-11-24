namespace MARS.Twitch.Infrastructure.Services.HelloVideos;

/// <summary>
/// Interface for sending hello video alerts.
/// Abstracts the delivery of hello video alerts to the OBS source.
/// </summary>
public interface IHelloVideoNotifier
{
    Task SendAlertAsync(HelloVideoAlertDto alert);
}
