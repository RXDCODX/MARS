namespace MARS.MediaStorage.Application.Media;

public interface IMediaInspector
{
    Task<MediaProbeResult> ProbeAsync(
        string filePath,
        CancellationToken cancellationToken = default
    );
}
