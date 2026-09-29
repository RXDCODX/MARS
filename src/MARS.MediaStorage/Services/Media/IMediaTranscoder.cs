namespace MARS.MediaStorage.Services.Media;

public interface IMediaTranscoder
{
    Task<string> EnsurePlayableAsync(
        string sourceFullPath,
        CancellationToken cancellationToken = default
    );
}
