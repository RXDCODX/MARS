namespace MARS.MediaStorage.Application.Media;

public interface IMediaTranscoder
{
    Task<string> EnsurePlayableAsync(
        string sourceFullPath,
        CancellationToken cancellationToken = default
    );
}
