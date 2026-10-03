using MARS.MediaStorage.Services.Git;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 1: инициализатор при старте.
/// <list type="bullet">
/// <item>при выключенной подсистеме не выполняет ничего и не падает;</item>
/// <item>при включённой и некорректной конфигурации падает сразу на старте,
/// а не молча ждёт первого события файла.</item>
/// </list>
/// </summary>
public class MediaGitInitializerTests
{
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotThrow()
    {
        var options = new MediaGitOptions { Enabled = false };
        var initializer = new MediaGitInitializer(
            new ThrowingGitService(),
            options,
            NullLogger<MediaGitInitializer>.Instance
        );

        await initializer.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WhenEnabledAndMisconfigured_Throws()
    {
        var options = new MediaGitOptions { Enabled = true, RepositoryUrl = "" };
        var initializer = new MediaGitInitializer(
            new ThrowingGitService(),
            options,
            NullLogger<MediaGitInitializer>.Instance
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.StartAsync(CancellationToken.None)
        );

        Assert.Contains("RepositoryUrl", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAsync_WhenEnabledAndRejectsScheme_Throws()
    {
        var options = new MediaGitOptions { Enabled = true, RepositoryUrl = "ext::sh -c evil" };
        var initializer = new MediaGitInitializer(
            new ThrowingGitService(),
            options,
            NullLogger<MediaGitInitializer>.Instance
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.StartAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task StartAsync_WhenEnabledAndReady_DoesNotThrow()
    {
        var options = new MediaGitOptions
        {
            Enabled = true,
            RepositoryUrl = "https://github.com/RXDCODX/random-memes.git",
        };
        var service = new StubGitService(new GitSyncResult(true, true, false, true, null));
        var initializer = new MediaGitInitializer(
            service,
            options,
            NullLogger<MediaGitInitializer>.Instance
        );

        await initializer.StartAsync(CancellationToken.None);

        Assert.Equal(1, service.InitializeCalls);
    }

    [Fact]
    public async Task StartAsync_WhenCloneFails_DoesNotTakeServiceDown()
    {
        // Регрессия: раньше сетевой сбой бросался из StartAsync, и при
        // restart: unless-stopped контейнер уходил в бесконечный crash-loop —
        // вместе с отдачей медиа. Сбой remote не должен ронять хранилище.
        var options = new MediaGitOptions
        {
            Enabled = true,
            RepositoryUrl = "https://github.com/RXDCODX/random-memes.git",
        };
        var service = new StubGitService(GitSyncResult.Failed("remote unreachable"));
        var initializer = new MediaGitInitializer(
            service,
            options,
            NullLogger<MediaGitInitializer>.Instance
        );

        await initializer.StartAsync(CancellationToken.None);

        Assert.Equal(1, service.InitializeCalls);
    }

    private sealed class ThrowingGitService : IMediaGitService
    {
        public Task<GitSyncResult> EnsureInitializedAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("git не должен вызываться при Enabled=false");

        public Task<GitSyncResult> SyncAsync(
            string message,
            bool allowEmptyCommit = false,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(GitSyncResult.NotAttempted());

        public Task<GitStatusResult> GetStatusAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new GitStatusResult(false, [], null, null));
    }

    private sealed class StubGitService(GitSyncResult initResult) : IMediaGitService
    {
        public int InitializeCalls { get; private set; }

        public Task<GitSyncResult> EnsureInitializedAsync(
            CancellationToken cancellationToken = default
        )
        {
            InitializeCalls++;

            return Task.FromResult(initResult);
        }

        public Task<GitSyncResult> SyncAsync(
            string message,
            bool allowEmptyCommit = false,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(initResult);

        public Task<GitStatusResult> GetStatusAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new GitStatusResult(true, [], "master", null));
    }

    /// <summary>
    /// Остановка инициализатора ничего не делает: git-синхронизация разовая, и
    /// ждать её на остановке нельзя — иначе сервис зависал бы при выключении.
    /// </summary>
    [Fact]
    public async Task StopIsSafeAfterStart()
    {
        var initializer = new MediaGitInitializer(
            Mock.Of<IMediaGitService>(),
            new MediaGitOptions { Enabled = false },
            NullLogger<MediaGitInitializer>.Instance
        );
        await initializer.StartAsync(TestContext.Current.CancellationToken);

        await initializer.StopAsync(TestContext.Current.CancellationToken);
    }
}
