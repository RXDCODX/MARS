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
    public async Task StartAsync_WarmsNestedVisibleFileTree_AndInvalidateRefreshesIt()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/clips/2026/final.mp4");
        context.WriteFile("root-image.png");
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

        Assert.Equal(["root-image.png"], tree.Files.Select(file => file.Name));
        Assert.Equal(["Alerts", "Empty"], tree.Directories.Select(directory => directory.Name));
        var alerts = Assert.Single(tree.Directories, directory => directory.Name == "Alerts");
        var clips = Assert.Single(alerts.Children);
        var year = Assert.Single(clips.Children);
        Assert.Equal("Alerts/clips/2026/final.mp4", Assert.Single(year.Files).RelativePath);
        Assert.DoesNotContain(
            tree.Directories,
            directory => directory.Name is ".hidden" or ".git" or "_trash"
        );

        Directory.CreateDirectory(Path.Combine(context.Root, "NewFolder"));
        var stillCached = await cache.GetSnapshotAsync(cancellationToken);
        Assert.DoesNotContain(stillCached.Directories, directory => directory.Name == "NewFolder");

        cache.Invalidate();
        var refreshed = await cache.GetSnapshotAsync(cancellationToken);
        Assert.Contains(refreshed.Directories, directory => directory.Name == "NewFolder");
        await cache.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task Snapshot_KeepsSiblingFolders_WhenOneFolderContainsManyFiles()
    {
        using var context = new StorageTestContext();
        for (var index = 0; index < 520; index++)
        {
            context.WriteFile($"Alerts/clip-{index:D4}.mp4");
        }

        context.WriteFile("Beta/keep.txt");

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MediaStorageDirectoryTreeCache(
            memoryCache,
            new StubWebHostEnvironment { WebRootPath = context.Root },
            NullLogger<MediaStorageDirectoryTreeCache>.Instance
        );
        var cancellationToken = TestContext.Current.CancellationToken;

        await cache.StartAsync(cancellationToken);
        var tree = await cache.GetSnapshotAsync(cancellationToken);

        var alerts = Assert.Single(tree.Directories, directory => directory.Name == "Alerts");
        var beta = Assert.Single(tree.Directories, directory => directory.Name == "Beta");
        Assert.Equal(100, alerts.Files.Count);
        Assert.Contains(beta.Files, file => file.RelativePath == "Beta/keep.txt");
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
