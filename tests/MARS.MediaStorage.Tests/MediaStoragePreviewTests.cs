using MARS.MediaStorage.Api.Rest.Controllers;
using MARS.MediaStorage.Api.Rest.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Tests;

public sealed class MediaStoragePreviewTests
{
    private static MediaStorageController CreateController(StorageTestContext context)
    {
        var environment = new StubWebHostEnvironment { WebRootPath = context.Root };
        return new MediaStorageController(context.CreateService(), environment, context.Git)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Fact]
    public async Task Preview_ReturnsInlineStreamWithRangeSupport()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/clip.mp4", "video-content");
        var controller = CreateController(context);

        var action = await controller.Preview(
            "Alerts/clip.mp4",
            TestContext.Current.CancellationToken
        );

        var result = Assert.IsType<FileStreamResult>(action);
        Assert.Equal("video/mp4", result.ContentType);
        Assert.True(result.EnableRangeProcessing);
        Assert.Equal("inline", controller.Response.Headers["Content-Disposition"]);
        Assert.Equal("nosniff", controller.Response.Headers["X-Content-Type-Options"]);
        await result.FileStream.DisposeAsync();
    }

    [Fact]
    public async Task GetFile_PreservesAttachmentAndRangeContract()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/clip.mp4", "video-content");
        var controller = CreateController(context);

        var action = await controller.GetFile(
            "Alerts/clip.mp4",
            TestContext.Current.CancellationToken
        );

        var result = Assert.IsType<FileStreamResult>(action);
        Assert.Equal("clip.mp4", result.FileDownloadName);
        Assert.True(result.EnableRangeProcessing);
        await result.FileStream.DisposeAsync();
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("script.js")]
    [InlineData("image.svg")]
    [InlineData("unknown.bin")]
    public async Task Preview_RejectsActiveAndUnknownFormats(string fileName)
    {
        using var context = new StorageTestContext();
        context.WriteFile(fileName);
        var action = await CreateController(context)
            .Preview(fileName, TestContext.Current.CancellationToken);

        var result = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, result.StatusCode);
    }

    [Fact]
    public async Task TextPreview_ReadsOnlyTheConfiguredLimitAndReportsTruncation()
    {
        using var context = new StorageTestContext();
        const int limit = 512 * 1024;
        context.WriteFile("Logs/application.log", new string('x', limit + 128));
        var action = await CreateController(context)
            .TextPreview("Logs/application.log", TestContext.Current.CancellationToken);

        var response = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<OperationResult<MediaStorageTextPreviewDto>>(response.Value);
        var preview = Assert.IsType<MediaStorageTextPreviewDto>(envelope.Result);

        Assert.True(preview.Truncated);
        Assert.Equal(limit + 128, preview.SizeBytes);
        Assert.Equal(limit, preview.Content.Length);
    }

    [Theory]
    [InlineData("../outside.txt", StatusCodes.Status400BadRequest)]
    [InlineData(".git/config", StatusCodes.Status404NotFound)]
    [InlineData("_trash/deleted.mp4", StatusCodes.Status404NotFound)]
    public async Task Preview_HidesUnsafeAndInternalPaths(string path, int expectedStatus)
    {
        using var context = new StorageTestContext();
        var action = await CreateController(context)
            .Preview(path, TestContext.Current.CancellationToken);

        var result = Assert.IsAssignableFrom<ObjectResult>(action);
        Assert.Equal(expectedStatus, result.StatusCode);
    }

    [Fact]
    public async Task Preview_RejectsSymbolicLinks()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var context = new StorageTestContext();
        var outside = Path.Combine(Path.GetTempPath(), $"mars-preview-{Guid.NewGuid():N}.mp4");
        await File.WriteAllTextAsync(outside, "outside", TestContext.Current.CancellationToken);
        try
        {
            var link = Path.Combine(context.Root, "shortcut.mp4");
            File.CreateSymbolicLink(link, outside);

            var action = await CreateController(context)
                .Preview("shortcut.mp4", TestContext.Current.CancellationToken);

            Assert.IsType<NotFoundObjectResult>(action);
        }
        finally
        {
            File.Delete(outside);
        }
    }

    private sealed class StubWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "MARS.MediaStorage";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
