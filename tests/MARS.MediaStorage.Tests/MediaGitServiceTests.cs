using MARS.MediaStorage.Services.Git;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 1: синхронизация wwwroot с внешним git-репозиторием.
/// Требования, зафиксированные этими тестами:
/// <list type="bullet">
/// <item>при выключенной подсистеме не происходит НИКАКИХ операций с git;</item>
/// <item>клонирование не затирает уже существующие файлы в каталоге;</item>
/// <item>токен не должен попасть в <c>.git/config</c> и в вывод команд;</item>
/// <item>пустое дерево изменений не должно создавать пустой коммит.</item>
/// </list>
/// Тесты работают против настоящего локального bare-репозитория, сети не нужно.
/// </summary>
public class MediaGitServiceTests : IDisposable
{
    private const string Token = "ghp_SUPERSECRETTOKEN123456";

    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mars-git-svc-" + Guid.NewGuid().ToString("N")
    );

    private string _remote = null!;
    private string _workDir = null!;

    public void Dispose()
    {
        // git создаёт объекты в .git с атрибутом ReadOnly, а Windows не удаляет
        // такие файлы рекурсивно — каталог пришлось бы оставлять в %TEMP%.
        ResetReadOnlyAttributes(_root);
        TryDeleteDirectory(_root);

        GC.SuppressFinalize(this);
    }

    private static void ResetReadOnlyAttributes(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attributes = File.GetAttributes(file);

                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void Arrange()
    {
        Directory.CreateDirectory(_root);
        _remote = Path.Combine(_root, "remote.git");
        _workDir = Path.Combine(_root, "wwwroot");
        Directory.CreateDirectory(_workDir);

        var executor = new GitCommandExecutor();
        var init = executor
            .RunAsync(
                _root,
                ["init", "--bare", "--initial-branch=master", _remote],
                cancellationToken: TestContext.Current.CancellationToken
            )
            .GetAwaiter()
            .GetResult();

        Assert.True(init.Success, init.StandardError);
    }

    private MediaGitOptions EnabledOptions(string? token = Token) =>
        new()
        {
            Enabled = true,
            RepositoryUrl = _remote,
            Branch = "master",
            Token = token,
            GitDataDirectory = Path.Combine(_root, "gitdata"),
        };

    private static string ReadGitConfigValue(string repoPath, string key)
    {
        var result = new GitCommandExecutor()
            .RunAsync(
                repoPath,
                ["config", "--get", key],
                cancellationToken: TestContext.Current.CancellationToken
            )
            .GetAwaiter()
            .GetResult();

        return result.StandardOutput.Trim();
    }

    private async Task WriteFileAsync(string relativePath, string content)
    {
        var full = Path.Combine(_workDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllTextAsync(full, content, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SyncAsync_WhenDisabled_DoesNothing()
    {
        Arrange();
        await File.WriteAllTextAsync(
            Path.Combine(_workDir, "keep.txt"),
            "x",
            TestContext.Current.CancellationToken
        );

        var service = new MediaGitService(
            new MediaGitOptions { Enabled = false, RepositoryUrl = _remote },
            new GitCommandExecutor(),
            _workDir
        );

        var result = await service.SyncAsync(
            "should not happen",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.False(result.Attempted);
        Assert.False(result.IsRepositoryReady);
        // Никаких git-артефактов создано быть не должно.
        Assert.False(Directory.Exists(Path.Combine(_workDir, ".git")));
        Assert.False(Directory.Exists(Path.Combine(_root, "gitdata")));
        Assert.True(File.Exists(Path.Combine(_workDir, "keep.txt")));
    }

    [Fact]
    public async Task EnsureInitializedAsync_WhenDisabled_DoesNothing()
    {
        Arrange();
        var service = new MediaGitService(
            new MediaGitOptions { Enabled = false, RepositoryUrl = _remote },
            new GitCommandExecutor(),
            _workDir
        );

        var result = await service.EnsureInitializedAsync(CancellationToken.None);

        Assert.False(result.Attempted);
        Assert.False(Directory.Exists(Path.Combine(_workDir, ".git")));
    }

    [Fact]
    public async Task EnsureInitializedAsync_CreatesRepositoryAtWorkDir()
    {
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);

        var result = await service.EnsureInitializedAsync(CancellationToken.None);

        Assert.True(result.Success, result.Error);
        Assert.True(result.Attempted);
        Assert.True(Directory.Exists(Path.Combine(_workDir, ".git")));
    }

    [Fact]
    public async Task EnsureInitializedAsync_DoesNotOverwriteExistingFiles()
    {
        // Аудит Stage 1: wwwroot не пустой — клон обязан сохранить содержимое,
        // иначе запуск сервиса стирал бы весь архив медиа.
        Arrange();
        var existing = Path.Combine(_workDir, "Alerts", "keep.mp4");
        Directory.CreateDirectory(Path.GetDirectoryName(existing)!);
        await File.WriteAllTextAsync(existing, "precious", TestContext.Current.CancellationToken);

        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);
        var result = await service.EnsureInitializedAsync(CancellationToken.None);

        Assert.True(result.Success, result.Error);
        Assert.True(File.Exists(existing));
        Assert.Equal(
            "precious",
            await File.ReadAllTextAsync(existing, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task EnsureInitializedAsync_IsIdempotent()
    {
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);

        var first = await service.EnsureInitializedAsync(CancellationToken.None);
        var second = await service.EnsureInitializedAsync(CancellationToken.None);

        Assert.True(first.Success, first.Error);
        Assert.True(second.Success, second.Error);
    }

    [Fact]
    public async Task EnsureInitializedAsync_NeverPersistsTokenToGitConfig()
    {
        // Аудит Stage 1: токен в .git/config означал бы его утечку в бэкап тома,
        // в образ и в любой дамп. В config должен лежать URL без учётных данных.
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);

        await service.EnsureInitializedAsync(CancellationToken.None);

        var remoteUrl = ReadGitConfigValue(_workDir, "remote.origin.url");
        Assert.NotEmpty(remoteUrl);
        Assert.DoesNotContain(Token, remoteUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SyncAsync_CommitsAndPushesNewFile()
    {
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);
        await service.EnsureInitializedAsync(CancellationToken.None);

        await WriteFileAsync("Alerts/videos/a.mp4", "video");

        var result = await service.SyncAsync(
            "add a.mp4",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(result.Success, result.Error);
        Assert.True(result.Committed);

        // Коммит обязан реально долететь в удалённый bare-репозиторий.
        var log = await new GitCommandExecutor().RunAsync(
            _root,
            ["--git-dir", _remote, "log", "--oneline"],
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(log.Success, log.StandardError);
        Assert.Contains("add a.mp4", log.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SyncAsync_WhenNothingChanged_DoesNotCreateEmptyCommit()
    {
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);
        await service.EnsureInitializedAsync(CancellationToken.None);

        // Первый коммит возможен только если есть что коммитить: на пустом
        // дереве git не создаёт коммит, и это штатное поведение.
        await WriteFileAsync("a.txt", "a");

        var first = await service.SyncAsync(
            "initial",
            cancellationToken: TestContext.Current.CancellationToken
        );
        var second = await service.SyncAsync(
            "nothing to do",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(first.Success, first.Error);
        Assert.True(first.Committed);
        Assert.True(second.Success, second.Error);
        Assert.False(second.Committed);

        var log = await new GitCommandExecutor().RunAsync(
            _root,
            ["--git-dir", _remote, "rev-list", "--count", "master"],
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal("1", log.StandardOutput.Trim());
    }

    [Fact]
    public async Task SyncAsync_UsesConfiguredAuthorIdentity()
    {
        Arrange();
        var options = EnabledOptions();
        options.AuthorName = "MARS Bot";
        options.AuthorEmail = "mars-bot@example.test";

        var service = new MediaGitService(options, new GitCommandExecutor(), _workDir);
        await service.EnsureInitializedAsync(CancellationToken.None);
        await WriteFileAsync("a.txt", "a");

        var result = await service.SyncAsync(
            "identity",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(result.Success, result.Error);

        var log = await new GitCommandExecutor().RunAsync(
            _root,
            ["--git-dir", _remote, "log", "-1", "--format=%an <%ae>"],
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("MARS Bot <mars-bot@example.test>", log.StandardOutput.Trim());
    }

    [Fact]
    public async Task SyncAsync_SetsCommitterIdentityFromOptions()
    {
        // `--author` задаёт только автора. Коммиттера git берёт из
        // user.name/user.email конфигурации, а в контейнере media-storage и на
        // раннере GitHub Actions глобальной идентичности нет — `git commit`
        // падал с «Committer identity unknown» и синк wwwroot не работал нигде,
        // кроме машин разработчиков. Коммиттер обязан приезжать из опций
        // сервиса, как и автор.
        Arrange();
        var options = EnabledOptions();
        options.AuthorName = "MARS Bot";
        options.AuthorEmail = "mars-bot@example.test";

        var service = new MediaGitService(options, new GitCommandExecutor(), _workDir);
        await service.EnsureInitializedAsync(CancellationToken.None);
        await WriteFileAsync("a.txt", "a");

        var result = await service.SyncAsync(
            "committer",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(result.Success, result.Error);

        var log = await new GitCommandExecutor().RunAsync(
            _root,
            ["--git-dir", _remote, "log", "-1", "--format=%cn <%ce>"],
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("MARS Bot <mars-bot@example.test>", log.StandardOutput.Trim());
    }

    [Fact]
    public async Task SyncAsync_RecordsDeletedFiles()
    {
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);
        await service.EnsureInitializedAsync(CancellationToken.None);

        var victim = Path.Combine(_workDir, "a.txt");
        await WriteFileAsync("a.txt", "a");
        await service.SyncAsync("add a", cancellationToken: TestContext.Current.CancellationToken);

        File.Delete(victim);
        var result = await service.SyncAsync(
            "remove a",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(result.Success, result.Error);
        Assert.True(result.Committed);

        var log = await new GitCommandExecutor().RunAsync(
            _root,
            ["--git-dir", _remote, "log", "-1", "--name-status"],
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Contains("remove a", log.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStatusAsync_WhenDisabled_ReportsNotReady()
    {
        Arrange();
        var service = new MediaGitService(
            new MediaGitOptions { Enabled = false },
            new GitCommandExecutor(),
            _workDir
        );

        var status = await service.GetStatusAsync(CancellationToken.None);

        Assert.False(status.IsRepositoryReady);
    }

    [Fact]
    public async Task GetStatusAsync_ReportsPendingChanges()
    {
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);
        await service.EnsureInitializedAsync(CancellationToken.None);
        await WriteFileAsync("a.txt", "a");

        var status = await service.GetStatusAsync(CancellationToken.None);

        Assert.True(status.IsRepositoryReady);
        Assert.Contains("a.txt", status.PendingPaths, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SyncAsync_ReportsFailureWhenPushRejected()
    {
        // Репозиторий только для чтения: push обязан вернуть ошибку, а не
        // тихо проглотить её — иначе сервис будет считать синхронизацию успешной.
        Arrange();
        var service = new MediaGitService(EnabledOptions(), new GitCommandExecutor(), _workDir);
        await service.EnsureInitializedAsync(CancellationToken.None);
        await WriteFileAsync("a.txt", "a");

        // Ломаем remote: переименовываем bare-репозиторий.
        Directory.Move(_remote, _remote + ".moved");

        var result = await service.SyncAsync(
            "will fail",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
