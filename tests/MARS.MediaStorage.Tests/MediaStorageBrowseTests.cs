using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Infrastructure.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MARS.MediaStorage.Tests;

public sealed class MediaStorageBrowseTests
{
    private static MediaStorageController CreateController(StorageTestContext context)
    {
        var environment = new StubWebHostEnvironment { WebRootPath = context.Root };
        var storage = context.CreateService();
        var browser = new FileSystemMediaStorageBrowserService(environment, storage);
        return new MediaStorageController(storage, environment, context.Git, browser);
    }

    [Fact]
    public async Task Browse_ReturnsEmptyAndNestedDirectoriesAndUnindexedFiles()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/clip.mp4");
        context.WriteFile("Alerts/nested/indexed.txt");
        Directory.CreateDirectory(Path.Combine(context.Root, "Alerts", "empty"));
        await context.CreateService().IndexAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(context);
        var action = await controller.Browse("Alerts", TestContext.Current.CancellationToken);
        var response = Assert.IsType<OkObjectResult>(action.Result);
        var result = Assert.IsType<OperationResult<MediaStorageBrowseResult>>(response.Value);

        Assert.True(result.Success);
        var listing = Assert.IsType<MediaStorageBrowseResult>(result.Result);
        Assert.Equal(["empty", "nested"], listing.Directories.Select(item => item.Name));
        var file = Assert.Single(listing.Files);
        Assert.Equal("clip.mp4", file.Name);
        Assert.True(file.IsIndexed);
        Assert.NotNull(file.StorageEntryId);
    }

    [Fact]
    public async Task Browse_ExcludesInternalAndHiddenEntries()
    {
        using var context = new StorageTestContext();
        context.WriteFile("visible.txt");
        context.WriteFile(".hidden.txt");
        context.WriteFile(".git/config");
        context.WriteFile("_trash/deleted.txt");
        Directory.CreateDirectory(Path.Combine(context.Root, ".private"));
        Directory.CreateDirectory(Path.Combine(context.Root, "_trash"));
        Directory.CreateDirectory(Path.Combine(context.Root, ".git"));

        var action = await CreateController(context).Browse(null, TestContext.Current.CancellationToken);
        var response = Assert.IsType<OkObjectResult>(action.Result);
        var result = Assert.IsType<OperationResult<MediaStorageBrowseResult>>(response.Value);

        var listing = Assert.IsType<MediaStorageBrowseResult>(result.Result);
        Assert.Equal(["visible.txt"], listing.Files.Select(item => item.Name));
        Assert.Empty(listing.Directories);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("Alerts/../../outside")]
    [InlineData("/etc")]
    [InlineData("C:/Windows")]
    [InlineData(".git")]
    [InlineData("_trash")]
    public async Task Browse_RejectsUnsafeDirectory(string directory)
    {
        using var context = new StorageTestContext();
        var action = await CreateController(context).Browse(
            directory,
            TestContext.Current.CancellationToken
        );

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }

    [Fact]
    public async Task Browse_ReturnsNotFoundForMissingDirectory()
    {
        using var context = new StorageTestContext();
        var action = await CreateController(context).Browse(
            "missing",
            TestContext.Current.CancellationToken
        );

        Assert.IsType<NotFoundObjectResult>(action.Result);
    }

    private class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "MARS.MediaStorage";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubWebHostEnvironment : StubHostEnvironment, IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
