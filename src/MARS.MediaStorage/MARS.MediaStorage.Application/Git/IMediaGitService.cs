namespace MARS.MediaStorage.Application.Git;

/// <summary>
/// Результат операции синхронизации.
/// </summary>
/// <param name="Attempted">Выполнялась ли операция вообще (при выключенной
/// подсистеме — <c>false</c>).</param>
/// <param name="Success">Успех ли завершилась операция.</param>
/// <param name="Committed">Был ли создан коммит (при отсутствии изменений — <c>false</c>).</param>
/// <param name="IsRepositoryReady">Готов ли репозиторий к работе.</param>
/// <param name="Error">Текст ошибки при неуспехе.</param>
public readonly record struct GitSyncResult(
    bool Attempted,
    bool Success,
    bool Committed,
    bool IsRepositoryReady,
    string? Error
)
{
    public static GitSyncResult NotAttempted(bool isRepositoryReady = false) =>
        new(false, true, false, isRepositoryReady, null);

    public static GitSyncResult Failed(string error, bool isRepositoryReady = false) =>
        new(true, false, false, isRepositoryReady, error);
}

/// <summary>
/// Состояние рабочей копии.
/// </summary>
public readonly record struct GitStatusResult(
    bool IsRepositoryReady,
    IReadOnlyList<string> PendingPaths,
    string? CurrentBranch,
    string? Error
);

public interface IMediaGitService
{
    Task<GitSyncResult> EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task<GitSyncResult> SyncAsync(
        string message,
        bool allowEmptyCommit = false,
        CancellationToken cancellationToken = default
    );

    Task<GitStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);
}
