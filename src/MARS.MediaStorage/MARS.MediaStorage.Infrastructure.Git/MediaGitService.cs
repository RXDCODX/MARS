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

    public async Task<GitSyncResult> ValidateRepositoryAsync(
        CancellationToken cancellationToken = default
    )
    {
        var problems = options.Validate();

        if (problems.Count > 0)
        {
            return GitSyncResult.Failed(string.Join("; ", problems));
        }

        if (!Directory.Exists(WorkDir))
        {
            return GitSyncResult.Failed($"Каталог media storage не существует: {WorkDir}");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var rootResult = await executor.RunAsync(
                WorkDir,
                ["rev-parse", "--show-toplevel"],
                cancellationToken: cancellationToken
            );

            if (!rootResult.Success)
            {
                return GitSyncResult.Failed(
                    $"Каталог '{WorkDir}' не является Git-репозиторием. "
                        + "Сначала клонируйте MEDIA_GIT_REPOSITORY_URL в MEDIA_STORAGE_HOST_PATH."
                );
            }

            var actualRoot = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(rootResult.StandardOutput.Trim())
            );
            var expectedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(WorkDir));
            var pathComparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!string.Equals(actualRoot, expectedRoot, pathComparison))
            {
                return GitSyncResult.Failed(
                    $"MEDIA_STORAGE_HOST_PATH должен быть корнем Git-репозитория. "
                        + $"Ожидался '{expectedRoot}', фактический корень — '{actualRoot}'."
                );
            }

            var remoteResult = await executor.RunAsync(
                WorkDir,
                ["remote", "get-url", options.RemoteName],
                cancellationToken: cancellationToken
            );

            if (!remoteResult.Success)
            {
                return GitSyncResult.Failed(
                    $"В локальном репозитории отсутствует remote '{options.RemoteName}'. "
                        + $"Ожидается репозиторий '{options.RepositoryUrl}'."
                );
            }

            var actualRemote = NormalizeRepositoryUrl(remoteResult.StandardOutput);
            var expectedRemote = NormalizeRepositoryUrl(options.RepositoryUrl);

            if (!string.Equals(actualRemote, expectedRemote, StringComparison.OrdinalIgnoreCase))
            {
                return GitSyncResult.Failed(
                    $"Неверный Git remote '{options.RemoteName}': настроен '{remoteResult.StandardOutput.Trim()}', "
                        + $"ожидается '{options.RepositoryUrl}'. Проверьте MEDIA_STORAGE_HOST_PATH."
                );
            }

            if (options.Enabled)
            {
                var branchResult = await executor.RunAsync(
                    WorkDir,
                    ["symbolic-ref", "--short", "HEAD"],
                    cancellationToken: cancellationToken
                );

                if (!branchResult.Success)
                {
                    return GitSyncResult.Failed(
                        "Рабочая копия должна находиться на локальной ветке, а не в detached HEAD."
                    );
                }

                var actualBranch = branchResult.StandardOutput.Trim();

                if (!string.Equals(actualBranch, options.Branch, StringComparison.Ordinal))
                {
                    return GitSyncResult.Failed(
                        $"В локальном репозитории выбрана ветка '{actualBranch}', "
                            + $"ожидается '{options.Branch}' (MEDIA_GIT_BRANCH)."
                    );
                }
            }

            return new GitSyncResult(true, true, false, true, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<GitSyncResult> EnsureInitializedAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!options.Enabled)
        {
            return GitSyncResult.NotAttempted(IsWorkDirRepository());
        }

        var validation = await ValidateRepositoryAsync(cancellationToken);

        return validation;
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
                cancellationToken: cancellationToken,
                environmentVariables: BuildGitEnvironment()
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

    private static string NormalizeRepositoryUrl(string url)
    {
        var normalized = url.Trim().Replace('\\', '/').TrimEnd('/');

        if (normalized.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        return normalized;
    }

    private IReadOnlyDictionary<string, string>? BuildGitEnvironment()
    {
        if (
            string.IsNullOrWhiteSpace(options.Token)
            || !options.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
        {
            return null;
        }

        // Не добавляем токен в URL remote, аргументы процесса или .git/config.
        // Git читает http.extraheader из переменных GIT_CONFIG_* только для push.
        var username = string.IsNullOrWhiteSpace(options.Username)
            ? "x-access-token"
            : options.Username;
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{username}:{options.Token}")
        );

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GIT_CONFIG_COUNT"] = "1",
            ["GIT_CONFIG_KEY_0"] = "http.extraheader",
            ["GIT_CONFIG_VALUE_0"] = $"Authorization: Basic {credentials}",
        };
    }

    private bool IsWorkDirRepository() =>
        Directory.Exists(Path.Combine(WorkDir, ".git"))
        || File.Exists(Path.Combine(WorkDir, ".git"));
}
