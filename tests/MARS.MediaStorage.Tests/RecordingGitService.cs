using MARS.TestKit.Postgres;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Заглушка git: считает коммиты, чтобы проверить, что каждое событие
/// хранилища порождает коммит, но не ходит в сеть.
/// </summary>
public sealed class RecordingGitService : IMediaGitService
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public int SyncCalls => _messages.Count;

    public List<IReadOnlyList<string>> PathSyncCalls { get; } = [];

    public Task<GitSyncResult> EnsureInitializedAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(new GitSyncResult(true, true, false, true, null));

    public Task<GitSyncResult> SyncAsync(
        string message,
        bool allowEmptyCommit = false,
        CancellationToken cancellationToken = default
    )
    {
        _messages.Add(message);
        return Task.FromResult(new GitSyncResult(true, true, true, true, null));
    }

    public Task<GitSyncResult> SyncPathsAsync(
        string message,
        IReadOnlyCollection<string> relativePaths,
        CancellationToken cancellationToken = default
    )
    {
        _messages.Add(message);
        PathSyncCalls.Add(relativePaths.ToArray());
        return Task.FromResult(new GitSyncResult(true, true, true, true, null));
    }

    public Task<GitStatusResult> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitStatusResult(true, [], "master", null));
}
