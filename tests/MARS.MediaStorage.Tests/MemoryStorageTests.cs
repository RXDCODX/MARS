using System.Text;
using MARS.MediaStorage.Controllers;
using MARS.MediaStorage.Services.MemoryStorageService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Хранилище в памяти и раздача файлов оверлею.
///
/// Хранилище статическое, поэтому тесты обязаны чистить его: иначе файлы одного
/// теста попадали бы в другой и счётчик использования уезжал бы вверх вместо
/// того, чтобы падать.
/// </summary>
/// <summary>
/// Хранилище в памяти статическое и общее для всего процесса, поэтому все
/// тесты, работающие с ним, обязаны идти в одной коллекции: иначе они идут
/// параллельно и видят файлы друг друга.
/// </summary>
[Collection("MemoryStorage")]
public sealed class MemoryStorageTests : IDisposable
{
    private static readonly byte[] Content = Encoding.UTF8.GetBytes("mars-memory-storage");

    private static readonly IServiceProvider MvcServices = CreateMvcServices();

    public void Dispose() => MemoryStorage.ClearStorage();

    /// <summary>
    /// MVC-результаты достают из запроса исполнители и логирование:
    /// FileStreamResult ищет IActionResultExecutor&lt;FileStreamResult&gt;,
    /// StatusCodeResult — ILoggerFactory. Контейнер собирается один раз,
    /// чтобы проверка не платила за MVC-регистрации в каждом тесте.
    /// </summary>
    private static IServiceProvider CreateMvcServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AddedFileBecomesReadableAndCounted()
    {
        await MemoryStorage.AddFileAsync("/alerts/alert.png", Content);

        Assert.True(MemoryStorage.FileExists("/alerts/alert.png"));
        Assert.Equal(1, MemoryStorage.FileCount);
        Assert.Equal((ulong)Content.Length, MemoryStorage.StorageSize);

        var (stream, contentType) = await MemoryStorage.GetFileStreamWithContentTypeAsync(
            "/alerts/alert.png"
        );
        Assert.Equal(Content, stream.ToArray());
        Assert.False(string.IsNullOrWhiteSpace(contentType));
    }

    /// <summary>
    /// Повторное добавление того же файла не заводит вторую запись, а увеличивает
    /// счётчик использования: оверлей может запросить один и тот же файл
    /// несколько раз, и содержимое не должно задваиваться.
    /// </summary>
    [Fact]
    public async Task RepeatedAddCountsUsageInsteadOfDuplicating()
    {
        await MemoryStorage.AddFileAsync("/alerts/alert.png", Content);
        var second = await MemoryStorage.AddFileAsync("/alerts/alert.png", Content);

        Assert.Equal(1, MemoryStorage.FileCount);
        Assert.Equal("/memory/alerts/alert.png", second);
        Assert.Equal((ulong)Content.Length, MemoryStorage.StorageSize);
    }

    /// <summary>
    /// Файл удаляется по последнему использованию: после одного Delete он ещё
    /// доступен, после второго исчезает.
    /// </summary>
    [Fact]
    public async Task FileIsRemovedOnlyAfterLastUse()
    {
        await MemoryStorage.AddFileAsync("/alerts/alert.png", Content);
        await MemoryStorage.AddFileAsync("/alerts/alert.png", Content);

        await MemoryStorage.DeleteFileAsync("/alerts/alert.png");
        Assert.True(MemoryStorage.FileExists("/alerts/alert.png"));

        await MemoryStorage.DeleteFileAsync("/alerts/alert.png");
        Assert.False(MemoryStorage.FileExists("/alerts/alert.png"));
        Assert.Equal(0, MemoryStorage.FileCount);
    }

    [Fact]
    public async Task AllFileNamesAreListedAndCleared()
    {
        await MemoryStorage.AddFileAsync("/a.png", Content);
        await MemoryStorage.AddFileAsync("/b.png", Content);

        var names = await MemoryStorage.GetAllFileNamesAsync();

        Assert.Equal(["/a.png", "/b.png"], names.OrderBy(name => name));

        await MemoryStorage.ClearStorageAsync();
        Assert.Equal(0, MemoryStorage.FileCount);
    }

    [Fact]
    public async Task MissingFileIsReported()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            MemoryStorage.GetFileStreamWithContentTypeAsync("/нет.png")
        );
        Assert.False(MemoryStorage.FileExists("/нет.png"));
        Assert.False(MemoryStorage.FileExists("   "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyNameIsRejected(string? fileName)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.AddFileAsync(fileName!, Content)
        );
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.DeleteFileAsync(fileName!)
        );
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.GetFileStreamWithContentTypeAsync(fileName!)
        );
    }

    /// <summary>
    /// Файл отдаётся потоком и после отдачи удаляется: оверлей забирает файл
    /// один раз, и держать его в памяти дальше незачем.
    /// </summary>
    /// <summary>
    /// Имя в хранилище совпадает с тем, что приходит в маршрут: файл кладут по
    /// пути «Alerts/photo.png», оверлей запрашивает «/memory/Alerts/photo.png»,
    /// и контроллер получает «Alerts/photo.png» без ведущего слэша.
    /// </summary>
    [Fact]
    public async Task OverlayEndpointServesAndDeletesFile()
    {
        await MemoryStorage.AddFileAsync("Alerts/alert.png", Content);
        var controller = new PyroAlerts
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = MvcServices },
            },
        };

        await controller.Index("Alerts/alert.png");

        Assert.False(MemoryStorage.FileExists("Alerts/alert.png"));
    }

    [Fact]
    public async Task OverlayEndpointAnswersBadRequestForUnknownFile()
    {
        var controller = new PyroAlerts
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = MvcServices },
            },
        };

        await controller.Index("нет-такого.png");

        Assert.Equal(StatusCodes.Status400BadRequest, controller.HttpContext.Response.StatusCode);
    }
}
