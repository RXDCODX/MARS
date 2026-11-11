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
    private readonly List<IReadOnlyList<string>> _pathSets = [];

    public IReadOnlyList<string> Messages => _messages;

    public IReadOnlyList<IReadOnlyList<string>> PathSets => _pathSets;

    public int SyncCalls => _messages.Count;

    public Task<GitSyncResult> ValidateRepositoryAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(new GitSyncResult(true, true, false, true, null));

    public Task<GitSyncResult> EnsureInitializedAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(new GitSyncResult(true, true, false, true, null));

    public Task<GitSyncResult> SyncAsync(
        string message,
        bool allowEmptyCommit = false,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<string>? paths = null
    )
    {
        _messages.Add(message);
        _pathSets.Add(paths?.ToArray() ?? []);
        return Task.FromResult(new GitSyncResult(true, true, true, true, null));
    }

    public Task<GitStatusResult> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitStatusResult(true, [], "master", null));
}
