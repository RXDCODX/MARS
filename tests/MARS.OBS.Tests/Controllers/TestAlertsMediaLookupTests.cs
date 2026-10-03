using System.Reflection;
using MARS.OBS.Controllers;
using MARS.Shared.Grpc.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;

namespace MARS.OBS.Tests.Controllers;

/// <summary>
/// Тестовый эндпоинт OBS: отдаёт любой файл из тома wwwroot по имени.
///
/// Это ручной способ проверить алерт на стенде: если файл не находится или
/// отдаётся не тот, ошибку видно сразу, без запуска OBS.
/// </summary>
public class TestAlertsMediaLookupTests : IDisposable
{
    private readonly string _root = Directory
        .CreateTempSubdirectory("mars-obs-test-alerts-")
        .FullName;

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Картинка находится в папке Alerts и возвращается путём от wwwroot: иначе
    /// тестовый эндпоинт не смог бы отдать файл.
    /// </summary>
    [Fact]
    public void ImageIsFoundUnderAlerts()
    {
        Write("Alerts/картинка.jpg");

        Assert.Equal("Alerts/картинка.jpg", Resolve("image"));
    }

    /// <summary>
    /// Кадры анимации лежат в отдельной папке faces: поиск в Alerts не нашёл бы их,
    /// и проверка анимации всегда отвечала бы «файла нет».
    /// </summary>
    [Fact]
    public void GifIsFoundUnderFaces()
    {
        Write("faces/анимация.gif");

        Assert.Equal("faces/анимация.gif", Resolve("gif"));
    }

    /// <summary>
    /// Отсутствующий файл даёт null, а не исключение: тестовый эндпоинт должен
    /// отвечать «нет файла» на любой запрос.
    /// </summary>
    [Fact]
    public void MissingFileResolvesToNull()
    {
        Assert.Null(Resolve("video"));
    }

    /// <summary>
    /// Неизвестный тип не ищется вообще: иначе эндпоинт отдавал бы файл вместо
    /// сообщения об ошибке.
    /// </summary>
    [Fact]
    public void UnknownTypeResolvesToNull()
    {
        Write("Alerts/картинка.jpg");

        Assert.Null(Resolve("неизвестный"));
    }

    /// <summary>
    /// Поиск файла приватный и берёт корень из окружения сервиса, поэтому
    /// подменяется только он — остальное окружение собирается заглушкой.
    /// </summary>
    private string? Resolve(string mediaType)
    {
        var controller = new TestAlertsController(
            Mock.Of<ITelegramusNotifier>(),
            new StubWebHostEnvironment { WebRootPath = _root }
        );
        var method = typeof(TestAlertsController).GetMethod(
            "FindFileForType",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (string?)method.Invoke(controller, [mediaType]);
    }

    private void Write(string relativePath)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "файл");
    }

    private class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";

        public string ApplicationName { get; set; } = "MARS.OBS";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubWebHostEnvironment : StubHostEnvironment, IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
