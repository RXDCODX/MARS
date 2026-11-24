namespace MARS.MediaStorage.Application.Git;

/// <summary>
/// Состояние рабочей копии.
/// </summary>
public readonly record struct GitStatusResult(
    bool IsRepositoryReady,
    IReadOnlyList<string> PendingPaths,
    string? CurrentBranch,
    string? Error
);
