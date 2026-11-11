using System.Text;
using MARS.MediaStorage.Api.Rest.Controllers;
using MARS.MediaStorage.Api.Rest.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace MARS.MediaStorage.Tests;

public sealed class MediaStorageFileActionTests
{
    private static MediaStorageController CreateController(StorageTestContext context)
    {
        var environment = new StubWebHostEnvironment { WebRootPath = context.Root };
        return new MediaStorageController(context.CreateService(), environment, context.Git)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    private static FormFile CreateFormFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream",
        };
    }

    [Fact]
    public async Task DeleteFile_IndexesAndMovesAnUnindexedPhysicalFileToTrash()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/unindexed.mp4", "video");
        var controller = CreateController(context);

        var action = await controller.DeleteFile(
            "Alerts/unindexed.mp4",
            TestContext.Current.CancellationToken
        );

        var response = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<OperationResult<BulkOperationResultDto>>(response.Value);
        Assert.True(envelope.Success);
        Assert.False(context.Exists("Alerts/unindexed.mp4"));

        var entries = await context
            .CreateService()
            .ListAsync(
                includeDeleted: true,
                cancellationToken: TestContext.Current.CancellationToken
            );
        var deleted = Assert.Single(entries.Where(entry => entry.IsDeleted));
        Assert.Equal("Alerts/unindexed.mp4", deleted.OriginalPath);
    }

    [Fact]
    public async Task ReplaceFile_UsesOriginalNameAndKeepsPreviousVersionInTrash()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/original.mp4", "old-version");
        var controller = CreateController(context);
        var replacement = CreateFormFile("replacement-with-another-name.mp4", "new-version");

        var action = await controller.ReplaceFile(
            "Alerts/original.mp4",
            replacement,
            TestContext.Current.CancellationToken
        );

        var response = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<OperationResult<BulkOperationResultDto>>(response.Value);
        Assert.True(envelope.Success);
        Assert.Equal(
            "new-version",
            await File.ReadAllTextAsync(
                Path.Combine(context.Root, "Alerts", "original.mp4"),
                TestContext.Current.CancellationToken
            )
        );

        var entries = await context
            .CreateService()
            .ListAsync(
                includeDeleted: true,
                cancellationToken: TestContext.Current.CancellationToken
            );
        Assert.Contains(entries, entry => entry.Path == "Alerts/original.mp4" && !entry.IsDeleted);
        Assert.Contains(
            entries,
            entry => entry.IsDeleted && entry.OriginalPath == "Alerts/original.mp4"
        );
    }

    [Fact]
    public async Task ReplaceFile_RestoresOriginalWhenReplacementExceedsUploadLimit()
    {
        using var context = new StorageTestContext { MaxUploadBytes = 1 };
        context.WriteFile("original.txt", "original-content");
        var controller = CreateController(context);
        var replacement = CreateFormFile("replacement.txt", "too-large");

        var action = await controller.ReplaceFile(
            "original.txt",
            replacement,
            TestContext.Current.CancellationToken
        );

        var response = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<OperationResult<BulkOperationResultDto>>(response.Value);
        Assert.False(envelope.Success);
        Assert.True(context.Exists("original.txt"));
        Assert.Equal(
            "original-content",
            await File.ReadAllTextAsync(
                Path.Combine(context.Root, "original.txt"),
                TestContext.Current.CancellationToken
            )
        );

        var entries = await context
            .CreateService()
            .ListAsync(
                includeDeleted: true,
                cancellationToken: TestContext.Current.CancellationToken
            );
        var active = Assert.Single(entries.Where(entry => !entry.IsDeleted));
        Assert.Equal("original.txt", active.Path);
        Assert.DoesNotContain(entries, entry => entry.IsDeleted);
    }

    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "MARS.MediaStorage";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
