using MARS.MediaStorage.Controllers;
using MARS.MediaStorage.Services.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: отдача файла по пути из UI — новая точка чтения произвольного
/// пути внутри wwwroot. Без проверок «../» позволял читать файлы вне
/// хранилища, а содержимое корзины отдавалось по прямой ссылке.
/// </summary>
public class MediaStorageFileEndpointTests
{
    private static MediaStorageController CreateController(StorageTestContext ctx)
    {
        var environment = new StubWebHostEnvironment { WebRootPath = ctx.Root };

        return new MediaStorageController(ctx.CreateService(), environment);
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("Alerts/../../secret.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/system32")]
    public async Task GetFile_RejectsPathsOutsideStorage(string path)
    {
        using var ctx = new StorageTestContext();
        var controller = CreateController(ctx);

        var result = await controller.GetFile(path);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetFile_RejectsTrashContent()
    {
        // Удалённый файл не должен открываться по прямой ссылке даже внутри
        // хранилища: иначе мягкое удаление не даёт никакого эффекта.
        using var ctx = new StorageTestContext();
        ctx.WriteFile("_trash/2026-03-15/deadbeef/a.mp4");
        var controller = CreateController(ctx);

        var result = await controller.GetFile("_trash/2026-03-15/deadbeef/a.mp4");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetFile_Returns404ForMissingFile()
    {
        using var ctx = new StorageTestContext();
        var controller = CreateController(ctx);

        var result = await controller.GetFile("Alerts/нет-такого.mp4");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetFile_RejectsEmptyPath()
    {
        using var ctx = new StorageTestContext();
        var controller = CreateController(ctx);

        Assert.IsType<BadRequestObjectResult>(await controller.GetFile("  "));
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
