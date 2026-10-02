namespace MARS.OBS.Services;

public interface IObsService
{
    bool IsConnected { get; }
    bool IsPaused { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<string> ScreenshotAsync(
        string? sourceName = null,
        CancellationToken cancellationToken = default
    );
    Task<ObsPauseResult> FreezeAsync(CancellationToken cancellationToken = default);
    Task<ObsPauseResult> UnfreezeAsync(CancellationToken cancellationToken = default);
    Task<ObsPauseResult> SwitchToPauseSceneAsync(CancellationToken cancellationToken = default);
    Task<ObsPauseResult> SwitchFromPauseSceneAsync(CancellationToken cancellationToken = default);
    Task<ObsPauseResult> TogglePauseAsync(
        ObsPauseMode mode = ObsPauseMode.FreezeFrame,
        CancellationToken cancellationToken = default
    );
}
