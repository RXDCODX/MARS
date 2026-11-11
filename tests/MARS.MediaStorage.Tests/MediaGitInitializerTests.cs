using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Проверка обязательной рабочей копии: наличие и remote проверяются даже
/// когда автоматическая Git-синхронизация отключена.
/// </summary>
public class MediaGitInitializerTests
{
    private const string RepositoryUrl = "https://github.com/RXDCODX/random-memes.git";

    [Fact]
    public async Task StartAsync_WhenGitSyncDisabled_StillValidatesMountedRepository()
    {
        var service = new StubGitService(new GitSyncResult(true, true, false, true, null));
        var initializer = new MediaGitInitializer(
            service,
            new MediaGitOptions { Enabled = false, RepositoryUrl = RepositoryUrl },
            NullLogger<MediaGitInitializer>.Instance
        );

        await initializer.StartAsync(CancellationToken.None);

        Assert.Equal(1, service.ValidationCalls);
        Assert.Equal(0, service.InitializeCalls);
    }

    [Fact]
    public async Task StartAsync_WhenRepositoryUrlIsMissing_ThrowsBeforeValidation()
    {
        var service = new ThrowingGitService();
        var initializer = new MediaGitInitializer(
            service,
            new MediaGitOptions { Enabled = false },
            NullLogger<MediaGitInitializer>.Instance
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.StartAsync(CancellationToken.None)
        );

        Assert.Contains("RepositoryUrl", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAsync_WhenRepositoryUrlUsesRejectedScheme_Throws()
    {
        var service = new ThrowingGitService();
        var initializer = new MediaGitInitializer(
            service,
            new MediaGitOptions { Enabled = false, RepositoryUrl = "ext::sh -c evil" },
            NullLogger<MediaGitInitializer>.Instance
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.StartAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task StartAsync_WhenEnabledAndRepositoryMatches_DoesNotThrow()
    {
        var service = new StubGitService(new GitSyncResult(true, true, false, true, null));
        var initializer = new MediaGitInitializer(
            service,
            new MediaGitOptions
            {
                Enabled = true,
                RepositoryUrl = RepositoryUrl,
                Branch = "master",
            },
            NullLogger<MediaGitInitializer>.Instance
        );

        await initializer.StartAsync(CancellationToken.None);

        Assert.Equal(1, service.ValidationCalls);
        Assert.Equal(0, service.InitializeCalls);
    }

    [Fact]
    public async Task StartAsync_WhenRepositoryIsMissing_StopsApplication()
    {
        var service = new StubGitService(
            GitSyncResult.Failed(
                "Каталог '/app/wwwroot' не является Git-репозиторием. "
                    + "Сначала клонируйте MEDIA_GIT_REPOSITORY_URL."
            )
        );
        var initializer = new MediaGitInitializer(
            service,
            new MediaGitOptions { Enabled = false, RepositoryUrl = RepositoryUrl },
            NullLogger<MediaGitInitializer>.Instance
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.StartAsync(CancellationToken.None)
        );

        Assert.Contains("MEDIA_STORAGE_HOST_PATH", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, service.ValidationCalls);
    }

    [Fact]
    public async Task StartAsync_WhenRepositoryValidationFails_StopsApplicationEvenIfGitSyncDisabled()
    {
        var service = new StubGitService(GitSyncResult.Failed("Неверный Git remote"));
        var initializer = new MediaGitInitializer(
            service,
            new MediaGitOptions { Enabled = false, RepositoryUrl = RepositoryUrl },
            NullLogger<MediaGitInitializer>.Instance
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.StartAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task StopIsSafeAfterStart()
    {
        var initializer = new MediaGitInitializer(
            new StubGitService(new GitSyncResult(true, true, false, true, null)),
            new MediaGitOptions { Enabled = false, RepositoryUrl = RepositoryUrl },
            NullLogger<MediaGitInitializer>.Instance
        );

        await initializer.StartAsync(TestContext.Current.CancellationToken);
        await initializer.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class ThrowingGitService : IMediaGitService
    {
        public Task<GitSyncResult> ValidateRepositoryAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Конфигурация должна быть проверена до Git.");

        public Task<GitSyncResult> EnsureInitializedAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Git init не должен вызываться.");

        public Task<GitSyncResult> SyncAsync(
            string message,
            bool allowEmptyCommit = false,
            CancellationToken cancellationToken = default,
            IReadOnlyCollection<string>? paths = null
        ) => Task.FromResult(GitSyncResult.NotAttempted());

        public Task<GitStatusResult> GetStatusAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new GitStatusResult(false, [], null, null));
    }

    private sealed class StubGitService(GitSyncResult validationResult) : IMediaGitService
    {
        public int ValidationCalls { get; private set; }

        public int InitializeCalls { get; private set; }

        public Task<GitSyncResult> ValidateRepositoryAsync(
            CancellationToken cancellationToken = default
        )
        {
            ValidationCalls++;
            return Task.FromResult(validationResult);
        }

        public Task<GitSyncResult> EnsureInitializedAsync(
            CancellationToken cancellationToken = default
        )
        {
            InitializeCalls++;
            return Task.FromResult(validationResult);
        }

        public Task<GitSyncResult> SyncAsync(
            string message,
            bool allowEmptyCommit = false,
            CancellationToken cancellationToken = default,
            IReadOnlyCollection<string>? paths = null
        ) => Task.FromResult(validationResult);

        public Task<GitStatusResult> GetStatusAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new GitStatusResult(true, [], "master", null));
    }
}
