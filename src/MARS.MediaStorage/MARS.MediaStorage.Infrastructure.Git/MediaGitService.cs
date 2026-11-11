using MARS.MediaStorage.Application.Git;

namespace MARS.MediaStorage.Infrastructure.Git;

/// <summary>
/// Синхронизирует каталог wwwroot с внешним git-репозиторием.
/// </summary>
/// <remarks>
/// Аудит Stage 1: операции с внешним репозиторием должны быть безопасны по
/// умолчанию и не должны затирать уже существующие файлы. Клонирование
/// выполняется во временный каталог, после чего содержимое переносится в
/// wwwroot с пропуском уже присутствующих файлов.
/// </remarks>
public sealed class MediaGitService(
    MediaGitOptions options,
    IGitCommandExecutor executor,
    string? workDirectory = null
) : IMediaGitService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Рабочая копия — каталог wwwroot. В контейнере это /app/wwwroot.
    /// </summary>
    private string WorkDir { get; } =
        string.IsNullOrWhiteSpace(workDirectory) ? Directory.GetCurrentDirectory() : workDirectory;

    public async Task<GitSyncResult> EnsureInitializedAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!options.Enabled)
        {
            return GitSyncResult.NotAttempted();
        }

        var problems = options.Validate();
        if (problems.Count > 0)
        {
            return GitSyncResult.Failed(string.Join("; ", problems));
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (IsWorkDirRepository())
            {
                return new GitSyncResult(true, true, false, true, null);
            }

            var cloneDir = CreateTemporaryCloneDirectory();

            // Аудит Stage 1: `clone --branch X` падает на пустом удалённом
            // репозитории («Remote branch X not found»), потому что ветка там
            // ещё не родилась. Клонируем без указания ветки, а HEAD переводим
            // на нужную через symbolic-ref — это работает и с существующей
            // веткой, и с пустым репозиторием.
            var clone = await executor.RunAsync(
                Path.GetDirectoryName(cloneDir)!,
                ["clone", BuildAuthenticatedUrl(), cloneDir],
                cancellationToken: cancellationToken
            );

            if (!clone.Success)
            {
                return GitSyncResult.Failed(clone.StandardError.Trim());
            }

            var head = await executor.RunAsync(
                cloneDir,
                ["symbolic-ref", "HEAD", $"refs/heads/{options.Branch}"],
                cancellationToken: cancellationToken
            );

            if (!head.Success)
            {
                return GitSyncResult.Failed(head.StandardError.Trim());
            }

            MoveCloneContentInto(cloneDir, WorkDir);

            var gitDir = Path.Combine(WorkDir, ".git");
            if (Directory.Exists(gitDir))
            {
                Directory.Delete(gitDir, true);
            }

            Directory.Move(Path.Combine(cloneDir, ".git"), gitDir);

            TryDeleteDirectory(cloneDir);

            // URL в .git/config уже без учётных данных — он и есть результат клона.
            var config = await executor.RunAsync(
                WorkDir,
                ["config", "remote.origin.url", options.RepositoryUrl],
                cancellationToken: cancellationToken
            );

            if (!config.Success)
            {
                return GitSyncResult.Failed(config.StandardError.Trim(), true);
            }

            return new GitSyncResult(true, true, false, true, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<GitSyncResult> SyncAsync(
        string message,
        bool allowEmptyCommit = false,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<string>? paths = null
    )
    {
        if (!options.Enabled)
        {
            return GitSyncResult.NotAttempted(IsWorkDirRepository());
        }

        var initialized = await EnsureInitializedAsync(cancellationToken);
        if (!initialized.Success)
        {
            return initialized;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var commitResult = await CommitAsync(
                message,
                allowEmptyCommit,
                paths,
                cancellationToken
            );
            if (!commitResult.Success)
            {
                return commitResult;
            }

            if (!commitResult.Committed)
            {
                return new GitSyncResult(true, true, false, true, null);
            }

            var push = await executor.RunAsync(
                WorkDir,
                ["push", options.RemoteName, $"HEAD:refs/heads/{options.Branch}"],
                cancellationToken: cancellationToken
            );

            if (!push.Success)
            {
                return GitSyncResult.Failed($"push не выполнен: {push.StandardError.Trim()}", true);
            }

            return new GitSyncResult(true, true, true, true, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<GitStatusResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Enabled || !IsWorkDirRepository())
        {
            return new GitStatusResult(false, [], null, null);
        }

        var status = await executor.RunAsync(
            WorkDir,
            ["status", "--porcelain"],
            cancellationToken: cancellationToken
        );

        if (!status.Success)
        {
            return new GitStatusResult(false, [], null, status.StandardError.Trim());
        }

        var branch = await executor.RunAsync(
            WorkDir,
            ["rev-parse", "--abbrev-ref", "HEAD"],
            cancellationToken: cancellationToken
        );

        var pending = status
            .StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Length > 3 ? line[3..].Trim() : line.Trim())
            .Where(path => path.Length > 0)
            .ToArray();

        return new GitStatusResult(
            true,
            pending,
            branch.Success ? branch.StandardOutput.Trim() : null,
            null
        );
    }

    private async Task<GitSyncResult> CommitAsync(
        string message,
        bool allowEmptyCommit,
        IReadOnlyCollection<string>? paths,
        CancellationToken cancellationToken
    )
    {
        // Пустой список — явный запрос «ничего не синхронизировать».
        // null оставлен для обратной совместимости: полная синхронизация дерева.
        if (paths is { Count: 0 })
        {
            return new GitSyncResult(true, true, false, true, null);
        }

        string[]? pathspecs = null;

        if (paths is not null)
        {
            var resolved = await ResolvePathspecsAsync(paths, cancellationToken);
            if (resolved.Error is not null)
            {
                return GitSyncResult.Failed(resolved.Error, true);
            }

            pathspecs = resolved.Paths;
            if (pathspecs.Length == 0)
            {
                return new GitSyncResult(true, true, false, true, null);
            }
        }

        var addArguments = new List<string> { "add", "--all" };
        if (pathspecs is not null)
        {
            addArguments.Add("--");
            addArguments.AddRange(pathspecs);
        }

        var add = await executor.RunAsync(
            WorkDir,
            addArguments,
            cancellationToken: cancellationToken
        );

        if (!add.Success)
        {
            return GitSyncResult.Failed($"git add: {add.StandardError.Trim()}", true);
        }

        // Идентичность коммиттера задаётся опциями сервиса, а pathspec ограничивает
        // коммит только выбранными файлами, не затрагивая чужие staged-изменения.
        var commitArguments = new List<string>
        {
            "-c",
            $"user.name={options.AuthorName}",
            "-c",
            $"user.email={options.AuthorEmail}",
            "commit",
            "--file=-",
            $"--author={options.AuthorName} <{options.AuthorEmail}>",
        };

        if (allowEmptyCommit)
        {
            commitArguments.Add("--allow-empty");
        }

        if (pathspecs is not null)
        {
            commitArguments.Add("--only");
            commitArguments.Add("--");
            commitArguments.AddRange(pathspecs);
        }

        var commit = await executor.RunAsync(
            WorkDir,
            commitArguments,
            stdin: message,
            cancellationToken: cancellationToken
        );

        if (!commit.Success)
        {
            var error = commit.StandardError + commit.StandardOutput;
            if (error.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
            {
                return new GitSyncResult(true, true, false, true, null);
            }

            return GitSyncResult.Failed($"git commit: {error.Trim()}", true);
        }

        return new GitSyncResult(true, true, true, true, null);
    }

    private async Task<(string[] Paths, string? Error)> ResolvePathspecsAsync(
        IReadOnlyCollection<string> paths,
        CancellationToken cancellationToken
    )
    {
        var normalizedPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ([], "Список путей для Git содержит пустой элемент");
            }

            var normalized = path.Replace('\\', '/');
            var segments = normalized.Split('/');
            var firstSegment = segments[0];
            var hasDrivePrefix =
                firstSegment.Length >= 2
                && char.IsLetter(firstSegment[0])
                && firstSegment[1] == ':';

            if (
                normalized.StartsWith("/", StringComparison.Ordinal)
                || Path.IsPathRooted(normalized)
                || hasDrivePrefix
                || segments.Any(segment => segment.Length == 0 || segment is "." or "..")
            )
            {
                return ([], $"Недопустимый относительный путь для Git: '{path}'");
            }

            normalizedPaths.Add(normalized);
        }

        if (normalizedPaths.Count == 0)
        {
            return ([], null);
        }

        // literal pathspec не воспринимает скобки и glob-символы в имени как шаблон.
        var pathspecs = normalizedPaths.Select(path => $":(literal){path}").ToArray();
        var trackedResult = await executor.RunAsync(
            WorkDir,
            ["ls-files", "--cached", "-z", "--", .. pathspecs],
            cancellationToken: cancellationToken
        );

        if (!trackedResult.Success)
        {
            return ([], $"git ls-files: {trackedResult.StandardError.Trim()}");
        }

        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var trackedPaths = new HashSet<string>(
            trackedResult.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries),
            comparer
        );
        var root = Path.GetFullPath(WorkDir);
        var rootPrefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var selectedPathspecs = new List<string>();

        foreach (var path in normalizedPaths)
        {
            var fullPath = Path.GetFullPath(
                Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
            );

            if (!fullPath.StartsWith(rootPrefix, comparison))
            {
                return ([], $"Путь выходит за пределы хранилища: '{path}'");
            }

            // Существующий файл может быть новым; отсутствующий допустим только
            // если он tracked и Git должен зафиксировать его удаление.
            if (File.Exists(fullPath) || trackedPaths.Contains(path))
            {
                selectedPathspecs.Add($":(literal){path}");
            }
        }

        return (selectedPathspecs.ToArray(), null);
    }

    private string BuildAuthenticatedUrl()
    {
        if (string.IsNullOrWhiteSpace(options.Token))
        {
            return options.RepositoryUrl;
        }

        if (!options.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return options.RepositoryUrl;
        }

        // Токен передаётся только в момент обращения к remote и не сохраняется:
        // после клона .git/config перезаписывается чистым URL.
        var user = string.IsNullOrWhiteSpace(options.Username)
            ? "x-access-token"
            : options.Username;

        return $"https://{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(options.Token)}@"
            + options.RepositoryUrl["https://".Length..];
    }

    private bool IsWorkDirRepository() => Directory.Exists(Path.Combine(WorkDir, ".git"));

    private string CreateTemporaryCloneDirectory()
    {
        var gitData = string.IsNullOrWhiteSpace(options.GitDataDirectory)
            ? Path.Combine(Path.GetTempPath(), "mars-media-git")
            : options.GitDataDirectory;

        Directory.CreateDirectory(gitData);

        var cloneDir = Path.Combine(gitData, "clone-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path.GetDirectoryName(cloneDir)!);

        return cloneDir;
    }

    /// <summary>
    /// Переносит файлы клона в рабочий каталог, не затирая уже существующие.
    /// Локальная копия хранилища всегда выигрывает: клон из внешнего репозитория
    /// не должен удалять медиа, которого в нём ещё нет.
    /// </summary>
    private static void MoveCloneContentInto(string cloneDir, string targetDir)
    {
        foreach (var sourceFile in Directory.EnumerateFiles(cloneDir))
        {
            var name = Path.GetFileName(sourceFile);

            if (name == ".git" || name == ".gitignore")
            {
                continue;
            }

            var destination = Path.Combine(targetDir, name);

            if (File.Exists(destination))
            {
                continue;
            }

            File.Copy(sourceFile, destination, overwrite: false);
        }

        foreach (var sourceDirectory in Directory.EnumerateDirectories(cloneDir))
        {
            var name = Path.GetFileName(sourceDirectory);

            if (name == ".git")
            {
                continue;
            }

            var destination = Path.Combine(targetDir, name);
            Directory.CreateDirectory(destination);
            MoveCloneContentInto(sourceDirectory, destination);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
            // Каталог-времянка: его удаление не критично для работы сервиса.
        }
        catch (UnauthorizedAccessException) { }
    }
}
