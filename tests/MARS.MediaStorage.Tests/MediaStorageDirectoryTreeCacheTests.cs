using MARS.MediaStorage.Presentation.Storage.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.MediaStorage.Tests;

public sealed class MediaStorageDirectoryTreeCacheTests
{
    [Fact]
    public async Task StartAsync_WarmsNestedVisibleDirectoryTree_AndInvalidateRefreshesIt()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/clips/2026/final.mp4");
        context.WriteFile(".hidden/config.json");
        context.WriteFile(".git/config");
        context.WriteFile("_trash/deleted.txt");
        Directory.CreateDirectory(Path.Combine(context.Root, "Empty"));

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MediaStorageDirectoryTreeCache(
            memoryCache,
            new StubWebHostEnvironment { WebRootPath = context.Root },
            NullLogger<MediaStorageDirectoryTreeCache>.Instance
        );
        var cancellationToken = TestContext.Current.CancellationToken;

        await cache.StartAsync(cancellationToken);
        var tree = await cache.GetSnapshotAsync(cancellationToken);

        Assert.Equal(["Alerts", "Empty"], tree.Select(directory => directory.Name));
        var alerts = Assert.Single(tree.Where(directory => directory.Name == "Alerts"));
        var clips = Assert.Single(alerts.Children);
        Assert.Equal("Alerts/clips", clips.RelativePath);
        Assert.Equal("Alerts/clips/2026", Assert.Single(clips.Children).RelativePath);
        Assert.DoesNotContain(tree, directory => directory.Name is ".hidden" or ".git" or "_trash");

        Directory.CreateDirectory(Path.Combine(context.Root, "NewFolder"));
        var stillCached = await cache.GetSnapshotAsync(cancellationToken);
        Assert.DoesNotContain(stillCached, directory => directory.Name == "NewFolder");

        cache.Invalidate();
        var refreshed = await cache.GetSnapshotAsync(cancellationToken);
        Assert.Contains(refreshed, directory => directory.Name == "NewFolder");
        await cache.StopAsync(cancellationToken);
    }

    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "MARS.MediaStorage.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
