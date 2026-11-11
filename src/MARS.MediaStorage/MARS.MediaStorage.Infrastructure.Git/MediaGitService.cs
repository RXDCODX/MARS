using LibGit2Sharp;
using MARS.MediaStorage.Application.Git;

namespace MARS.MediaStorage.Infrastructure.Git;

/// <summary>
/// Синхронизирует заранее подготовленную Git-рабочую копию wwwroot с remote
/// через LibGit2Sharp, без запуска внешнего процесса git.
/// </summary>
/// <remarks>
/// Выборочные коммиты строятся в отдельном временном Git index, изначально
/// совпадающем с HEAD. Это сохраняет staged-изменения других файлов в основном
/// index и не позволяет случайно включить их в пользовательский коммит.
/// </remarks>
public sealed class MediaGitService(MediaGitOptions options, string? workDirectory = null)
    : IMediaGitService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string WorkDir { get; } =
        string.IsNullOrWhiteSpace(workDirectory) ? Directory.GetCurrentDirectory() : workDirectory;

    public async Task<GitSyncResult> ValidateRepositoryAsync(
        CancellationToken cancellationToken = default
    )
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(ValidateRepositoryCore, cancellationToken);
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

        return await ValidateRepositoryAsync(cancellationToken);
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

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var validation = ValidateRepositoryCore();
            if (!validation.Success)
            {
                return validation;
            }

            return await Task.Run(
                () => SyncCore(message, allowEmptyCommit, paths, cancellationToken),
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GitSyncResult.Failed(SanitizeError(ex), isRepositoryReady: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<GitStatusResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return new GitStatusResult(false, [], null, null);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(
                () =>
                {
                    using var repository = new Repository(WorkDir);
                    if (repository.Info.IsBare)
                    {
                        return new GitStatusResult(false, [], null, "Git repository is bare.");
                    }

                    var status = repository.RetrieveStatus(
                        new StatusOptions
                        {
                            IncludeUntracked = true,
                            RecurseUntrackedDirs = true,
                            DetectRenamesInIndex = true,
                            DetectRenamesInWorkDir = true,
                        }
                    );

                    var pending = status
                        .Where(entry => entry.State != FileStatus.Unaltered)
                        .Select(entry => entry.FilePath)
                        .Where(path => !string.IsNullOrWhiteSpace(path))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(path => path, StringComparer.Ordinal)
                        .ToArray();

                    var branch = repository.Info.IsHeadDetached
                        ? null
                        : repository.Head.FriendlyName;

                    return new GitStatusResult(true, pending, branch, null);
                },
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new GitStatusResult(false, [], null, SanitizeError(ex));
        }
        finally
        {
            _gate.Release();
        }
    }

    private GitSyncResult ValidateRepositoryCore()
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

        try
        {
            using var repository = new Repository(WorkDir);

            if (
                repository.Info.IsBare
                || string.IsNullOrWhiteSpace(repository.Info.WorkingDirectory)
            )
            {
                return GitSyncResult.Failed(
                    $"Каталог '{WorkDir}' должен быть корнем Git-рабочей копии, а не bare-репозиторием."
                );
            }

            var actualRoot = NormalizeDirectory(repository.Info.WorkingDirectory);
            var expectedRoot = NormalizeDirectory(WorkDir);
            var pathComparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!string.Equals(actualRoot, expectedRoot, pathComparison))
            {
                return GitSyncResult.Failed(
                    "MEDIA_STORAGE_HOST_PATH должен быть корнем Git-репозитория. "
                        + $"Ожидался '{expectedRoot}', фактический корень — '{actualRoot}'."
                );
            }

            using var remote = repository.Network.Remotes[options.RemoteName];
            if (remote is null)
            {
                return GitSyncResult.Failed(
                    $"В локальном репозитории отсутствует remote '{options.RemoteName}'. "
                        + $"Ожидается репозиторий '{options.RepositoryUrl}'."
                );
            }

            var expectedRemote = NormalizeRepositoryUrl(options.RepositoryUrl);
            var actualRemote = NormalizeRepositoryUrl(remote.Url);

            if (!string.Equals(actualRemote, expectedRemote, StringComparison.OrdinalIgnoreCase))
            {
                return GitSyncResult.Failed(
                    $"Неверный Git remote '{options.RemoteName}': настроен '{actualRemote}', "
                        + $"ожидается '{expectedRemote}'. Проверьте MEDIA_STORAGE_HOST_PATH."
                );
            }

            var actualPushRemote = NormalizeRepositoryUrl(remote.PushUrl);
            if (
                !string.Equals(actualPushRemote, expectedRemote, StringComparison.OrdinalIgnoreCase)
            )
            {
                return GitSyncResult.Failed(
                    $"Неверный push URL Git remote '{options.RemoteName}': настроен '{actualPushRemote}', "
                        + $"ожидается '{expectedRemote}'. Проверьте MEDIA_STORAGE_HOST_PATH."
                );
            }

            if (options.Enabled)
            {
                if (repository.Info.IsHeadDetached)
                {
                    return GitSyncResult.Failed(
                        "Рабочая копия должна находиться на локальной ветке, а не в detached HEAD."
                    );
                }

                var actualBranch = repository.Head.FriendlyName;
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
        catch (RepositoryNotFoundException)
        {
            return GitSyncResult.Failed(
                $"Каталог '{WorkDir}' не является Git-репозиторием. "
                    + "Сначала клонируйте MEDIA_GIT_REPOSITORY_URL в MEDIA_STORAGE_HOST_PATH."
            );
        }
        catch (Exception ex)
        {
            return GitSyncResult.Failed(SanitizeError(ex));
        }
    }

    private GitSyncResult SyncCore(
        string message,
        bool allowEmptyCommit,
        IReadOnlyCollection<string>? paths,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolved = ResolvePaths(paths);
        if (resolved.Error is not null)
        {
            return GitSyncResult.Failed(resolved.Error, isRepositoryReady: true);
        }

        // Пустой список — явный запрос ничего не синхронизировать. null означает
        // полную синхронизацию дерева и сохраняет прежнее поведение интерфейса.
        if (paths is { Count: 0 } || resolved.Paths is { Length: 0 } && paths is not null)
        {
            return new GitSyncResult(true, true, false, true, null);
        }

        using var repository = new Repository(WorkDir);
        using var remote =
            repository.Network.Remotes[options.RemoteName]
            ?? throw new InvalidOperationException(
                $"Git remote '{options.RemoteName}' исчез во время синхронизации."
            );

        var committed = false;
        ObjectId? newCommitId = null;
        var temporaryIndexDirectory = Directory.CreateTempSubdirectory("mars-media-git-");

        try
        {
            // Для выборочной отправки нельзя использовать обычный repository.Index:
            // там могут находиться чужие staged-файлы. Временный index всегда начинает
            // с HEAD и включает только явно переданные пути.
            var indexPath = Path.Combine(temporaryIndexDirectory.FullName, "index");
            using (
                var stagingRepository = new Repository(
                    WorkDir,
                    new RepositoryOptions { IndexPath = indexPath }
                )
            )
            {
                var parent = stagingRepository.Head.Tip;
                if (parent is not null)
                {
                    stagingRepository.Index.Replace(parent);
                    stagingRepository.Index.Write();
                }

                var stageOptions = new StageOptions
                {
                    ExplicitPathsOptions = new ExplicitPathsOptions
                    {
                        // Удалённый либо ещё не отслеживаемый путь не должен ломать
                        // массовую синхронизацию: Git решит, есть ли изменение в index.
                        ShouldFailOnUnmatchedPath = false,
                    },
                };

                if (resolved.Paths is null)
                {
                    // Без фильтра путей синхронизируется всё рабочее дерево.
                    // Сначала получаем изменённые пути, включая новые файлы, и
                    // передаём их в staging явно: "." не находит новые файлы
                    // при работе с отдельным индексом.
                    var changedPaths = stagingRepository
                        .Diff.Compare<TreeChanges>(paths: null, includeUntracked: true)
                        .Select(change => change.Path)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

                    if (changedPaths.Length > 0)
                    {
                        Commands.Stage(stagingRepository, changedPaths, stageOptions);
                    }
                }
                else
                {
                    Commands.Stage(stagingRepository, resolved.Paths, stageOptions);
                }

                cancellationToken.ThrowIfCancellationRequested();

                var hasChanges = parent is null
                    ? stagingRepository.Index.Count > 0
                    : HasIndexChanges(stagingRepository, parent);

                if (hasChanges || allowEmptyCommit)
                {
                    var now = DateTimeOffset.UtcNow;
                    var author = new Signature(options.AuthorName, options.AuthorEmail, now);
                    var newCommit = stagingRepository.Commit(
                        message,
                        author,
                        author,
                        new CommitOptions { AllowEmptyCommit = allowEmptyCommit }
                    );
                    newCommitId = newCommit.Id;
                    committed = true;
                }
            }
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryIndexDirectory.FullName, recursive: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Возвращаем в основной index только то, что действительно вошло в коммит.
        // Чужие staged-файлы остаются staged и не попадают ни в commit, ни в push.
        if (committed && newCommitId is not null)
        {
            var commitInMainRepository =
                repository.Lookup<Commit>(newCommitId)
                ?? throw new InvalidOperationException(
                    "LibGit2Sharp не смог перечитать созданный commit из основной рабочей копии."
                );

            if (resolved.Paths is null)
            {
                repository.Index.Replace(commitInMainRepository);
            }
            else
            {
                repository.Index.Replace(
                    commitInMainRepository,
                    resolved.Paths,
                    new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false }
                );
            }

            repository.Index.Write();
        }

        // Если предыдущий push завершился ошибкой, локальный HEAD уже содержит
        // коммит. Повторный вызов без новых изменений должен уметь отправить его.
        var headId = newCommitId ?? repository.Head.Tip?.Id;
        if (headId is null)
        {
            return new GitSyncResult(true, true, false, true, null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            repository.Network.Push(
                remote,
                $"{headId.Sha}:refs/heads/{options.Branch}",
                CreatePushOptions(remote, cancellationToken)
            );
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            return new GitSyncResult(
                true,
                false,
                committed,
                true,
                $"push не выполнен: {SanitizeError(ex)}"
            );
        }

        return new GitSyncResult(true, true, committed, true, null);
    }

    private static bool HasIndexChanges(Repository repository, Commit parent)
    {
        using var changes = repository.Diff.Compare<TreeChanges>(parent.Tree, DiffTargets.Index);

        return changes.Any();
    }

    private PushOptions CreatePushOptions(Remote remote, CancellationToken cancellationToken)
    {
        var pushOptions = new PushOptions
        {
            OnPushTransferProgress = (_, _, _) => !cancellationToken.IsCancellationRequested,
        };

        if (string.IsNullOrWhiteSpace(options.Token) || !IsHttpRepositoryUrl(remote.PushUrl))
        {
            return pushOptions;
        }

        pushOptions.CredentialsProvider = (_, usernameFromUrl, _) =>
            new UsernamePasswordCredentials
            {
                Username = string.IsNullOrWhiteSpace(options.Username)
                    ? string.IsNullOrWhiteSpace(usernameFromUrl)
                        ? "x-access-token"
                        : usernameFromUrl
                    : options.Username,
                Password = options.Token,
            };

        return pushOptions;
    }

    private (string[]? Paths, string? Error) ResolvePaths(IReadOnlyCollection<string>? paths)
    {
        if (paths is null)
        {
            return (null, null);
        }

        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var normalizedPaths = new HashSet<string>(comparer);
        var root = Path.GetFullPath(WorkDir);
        var rootPrefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

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

            var fullPath = Path.GetFullPath(
                Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar))
            );

            if (!fullPath.StartsWith(rootPrefix, comparison))
            {
                return ([], $"Путь выходит за пределы хранилища: '{path}'");
            }

            normalizedPaths.Add(normalized);
        }

        return (normalizedPaths.ToArray(), null);
    }

    private static string NormalizeDirectory(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool IsHttpRepositoryUrl(string? value) =>
        value?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true
        || value?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true;

    private static string NormalizeRepositoryUrl(string? url)
    {
        var normalized = (url ?? string.Empty).Trim().Replace('\\', '/').TrimEnd('/');

        // Не сравниваем и не выводим credentials, если remote был склонирован
        // с токеном в URL. Идентичность репозитория определяется host + path.
        if (
            Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "ssh", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            var builder = new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty };
            normalized = builder.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        }
        else if (!normalized.Contains("://", StringComparison.Ordinal))
        {
            // SCP-формат SSH: git@github.com:owner/repository.git.
            var at = normalized.IndexOf('@');
            var colon = normalized.IndexOf(':');
            if (at >= 0 && colon > at)
            {
                normalized = normalized[(at + 1)..];
            }
        }

        if (normalized.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        return normalized;
    }

    private string SanitizeError(Exception exception)
    {
        var message = exception.Message;
        if (!string.IsNullOrEmpty(options.Token))
        {
            message = message.Replace(options.Token, "***", StringComparison.Ordinal);
        }

        return message;
    }

    private bool IsWorkDirRepository()
    {
        if (!Directory.Exists(WorkDir))
        {
            return false;
        }

        try
        {
            using var repository = new Repository(WorkDir);
            return !repository.Info.IsBare;
        }
        catch
        {
            return false;
        }
    }
}
