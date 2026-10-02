namespace MARS.MediaStorage.Services.Media;

public interface IMediaInspector
{
    Task<MediaProbeResult> ProbeAsync(
        string filePath,
        CancellationToken cancellationToken = default
    );
}