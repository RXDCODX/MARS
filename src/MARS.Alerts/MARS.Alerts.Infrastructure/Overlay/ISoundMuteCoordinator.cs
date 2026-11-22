namespace MARS.Alerts.Infrastructure.Overlay;

/// <summary>
/// Управление общим mute для звуковых сервисов, инициируемое через оверлей.
/// </summary>
public interface ISoundMuteCoordinator
{
    Task MuteAsync(string[]? excludedProcessNames = null);

    Task UnmuteAsync();
}
