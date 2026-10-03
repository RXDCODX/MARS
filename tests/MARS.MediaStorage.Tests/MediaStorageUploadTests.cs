using System.Reflection;
using MARS.MediaStorage.Controllers;
using MARS.MediaStorage.Entities;
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

    /// <summary>
    /// Список записей отдаётся по возрастанию пути и без удалённых: иначе интерфейс
    /// хранилиша показывал бы удалённые мемы как обычные.
    /// </summary>
    [Fact]
    public async Task ListingSkipsSoftDeletedEntries()
    {
        using var ctx = new StorageTestContext();
        var service = ctx.CreateService();
        var alive = await UploadAsync(service, "Alerts/живой.jpg");
        var deleted = await UploadAsync(service, "Alerts/удалённый.jpg");
        await service.SoftDeleteAsync(
            [deleted.Id],
            cancellationToken: TestContext.Current.CancellationToken
        );

        var listed = await service.ListAsync(
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(["Alerts/живой.jpg"], listed.Select(entry => entry.Path).ToArray());
        Assert.Equal(alive.Id, Assert.Single(listed).Id);
    }

    /// <summary>
    /// С удалёнными включёнными запись остаётся в списке: иначе корзину нельзя
    /// было бы показать и восстановить файл из неё. Путь при этом меняется на
    /// корзину, а сам файл с места жительства убирается.
    /// </summary>
    [Fact]
    public async Task DeletedEntriesAreListedWhenAsked()
    {
        using var ctx = new StorageTestContext();
        var service = ctx.CreateService();
        var deleted = await UploadAsync(service, "Alerts/удалённый.jpg");

        await service.SoftDeleteAsync(
            [deleted.Id],
            cancellationToken: TestContext.Current.CancellationToken
        );

        var listed = await service.ListAsync(
            includeDeleted: true,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var entry = Assert.Single(listed);
        Assert.NotNull(entry.DeletedAt);
        Assert.StartsWith("_trash/", entry.Path, StringComparison.Ordinal);
        Assert.EndsWith("удалённый.jpg", entry.Path, StringComparison.Ordinal);
    }

    /// <summary>
    /// Загрузка одного файла: содержимое уникально для каждого пути, иначе вторая
    /// загрузка узнала бы первый файл по хешу и просто переименовала бы его.
    /// </summary>
    private static async Task<MediaStorageEntry> UploadAsync(
        MARS.MediaStorage.Services.Storage.IMediaStorageService service,
        string path
    )
    {
        var content = System.Text.Encoding.UTF8.GetBytes(path);
        var result = await service.UploadAsync(
            [
                new MediaUploadFile(
                    Path.GetFileName(path),
                    new MemoryStream(content),
                    "image/jpeg",
                    content.Length
                ),
            ],
            Path.GetDirectoryName(path)!,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, result.Succeeded);
        return (
            await service.ListAsync(cancellationToken: TestContext.Current.CancellationToken)
        ).Single(entry => entry.Path == path);
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
