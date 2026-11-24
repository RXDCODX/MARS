namespace MARS.MediaStorage.Application.Git;

public interface IMediaGitService
{
    Task<GitSyncResult> ValidateRepositoryAsync(CancellationToken cancellationToken = default);

    Task<GitSyncResult> EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task<GitSyncResult> SyncAsync(
        string message,
        bool allowEmptyCommit = false,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<string>? paths = null
    );

    Task<GitStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);
}
