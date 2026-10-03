using System.Reflection;
using MARS.MediaStorage.Controllers;
using MARS.MediaStorage.Entities.DTOs;
using MARS.MediaStorage.Services;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Загрузка файлов в хранилище и разбор путей отдачи.
///
/// Путь к файлу собирается из папки и имени: если он собирается неверно, файл не
/// отдастся по ссылке из интерфейса. Загрузка же должна отчитаться по каждому
/// файлу, включая пустые, иначе клиент не поймёт, куда делась его загрузка.
/// </summary>
public class MediaStorageUploadTests
{
    /// <summary>
    /// Загрузка без файлов отклоняется словами, а не пустым успехом: иначе
    /// интерфейс показал бы «готово», хотя ничего не загрузилось.
    /// </summary>
    [Fact]
    public async Task UploadWithoutFilesIsRejected()
    {
        using var ctx = new StorageTestContext();
        var controller = new MediaStorageController(ctx.CreateService(), Environment(ctx));

        var result = await controller.Upload(null, "Alerts", TestContext.Current.CancellationToken);

        var payload = Assert.IsType<OperationResult<BulkOperationResultDto>>(
            Assert.IsType<OkObjectResult>(result.Result).Value
        );
        Assert.False(payload.Success);
    }

    /// <summary>
    /// Пустой файл не пишется в хранилище, но попадает в отчёт об ошибках: иначе
    /// клиент видел бы «запрошено 1, обработано 0» и не понял бы причину.
    /// </summary>
    [Fact]
    public async Task EmptyFileIsReportedAsSkipped()
    {
        using var ctx = new StorageTestContext();
        var controller = new MediaStorageController(ctx.CreateService(), Environment(ctx));
        var files = new FormFileCollection
        {
            new FormFile(new MemoryStream(), 0, 0, "files", "пустой.mp4"),
        };

        var result = await controller.Upload(
            files,
            "Alerts",
            TestContext.Current.CancellationToken
        );

        var payload = Assert.IsType<OperationResult<BulkOperationResultDto>>(
            Assert.IsType<OkObjectResult>(result.Result).Value
        );
        Assert.True(payload.Success);
        Assert.Equal(1, payload.Result!.Requested);
        Assert.Equal(0, payload.Result.Succeeded);
        Assert.NotEmpty(payload.Result.Errors);
    }

    /// <summary>
    /// Путь к файлу собирается под wwwroot: иначе файл не находился бы там, куда
    /// его положили, и отдача мема завершалась бы 404.
    /// </summary>
    [Fact]
    public void FullPathIsResolvedUnderWebRoot()
    {
        using var ctx = new StorageTestContext();
        var controller = new RandomMemeController(
            Mock.Of<IRandomMemeService>(),
            Mock.Of<IMediaStorageService>(),
            Environment(ctx),
            NullLogger<RandomMemeController>.Instance
        );

        var path = Invoke(controller, "ResolveFullPath", ["Alerts", "мем.mp4"]);

        Assert.StartsWith(Path.GetFullPath(ctx.Root), path);
        Assert.EndsWith(Path.Combine("Alerts", "мем.mp4"), path);
    }

    private static string Invoke(object target, string name, object?[] arguments)
    {
        var method = target
            .GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        return (string)method.Invoke(target, arguments)!;
    }

    private static IWebHostEnvironment Environment(StorageTestContext ctx) =>
        new StubWebHostEnvironment { WebRootPath = ctx.Root };

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
