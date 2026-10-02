using System.Text;
using MARS.MediaStorage.Services.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Хранилище файлов в wwwroot поверх настоящей файловой системы во временном
/// каталоге.
///
/// Главное здесь — границы каталога: путь приходит из БД и из имени файла
/// запроса, поэтому «..» и абсолютные пути обязаны отбрасываться. Без этого
/// удаление «../../appsettings.json» снесло бы конфигурацию сервиса.
/// </summary>
public class WebRootMediaFileStorageServiceTests : IDisposable
{
    private readonly string _webRoot = Path.Combine(
        Path.GetTempPath(),
        "mars-webroot-" + Guid.NewGuid().ToString("N")
    );
    private readonly WebRootMediaFileStorageService _service;
    private readonly List<string> _createdPaths = [];

    public WebRootMediaFileStorageServiceTests()
    {
        Directory.CreateDirectory(_webRoot);

        var environment = new StubEnvironment(_webRoot);
        _service = new WebRootMediaFileStorageService(
            environment,
            NullLogger<WebRootMediaFileStorageService>.Instance
        );
    }

    public void Dispose()
    {
        foreach (var created in _createdPaths)
        {
            var relative = created.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            DeleteQuietly(Path.Combine(_webRoot, relative));
            DeleteQuietly(Path.Combine(DevWebRoot(), relative));
        }

        try
        {
            Directory.Delete(_webRoot, true);
        }
        catch (IOException) { }
    }

    /// <summary>
    /// Куда сервис копирует файлы для разработки: тот же поиск корня проекта,
    /// что и в production-коде. Копии удаляются вместе с исходниками, иначе тест
    /// оставлял бы файлы в wwwroot репозитория.
    /// </summary>
    private static string DevWebRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (directory.GetFiles("*.csproj").Length > 0)
            {
                return Path.Combine(directory.FullName, "wwwroot");
            }

            directory = directory.Parent;
        }

        return Path.GetTempPath();
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [Fact]
    public async Task FileIsSavedUnderGeneratedName()
    {
        var info = await _service.SaveFileAsync(FormFile("мем.mp4", "содержимое"));

        _createdPaths.Add(info.FilePath);
        Assert.Equal(".mp4", info.Extension);
        // Без подсказки имя генерируется: имя файла из запроса на диск не попадает.
        Assert.EndsWith(".mp4", info.FileName);
        Assert.StartsWith("/Alerts/uploaded_mems/", info.FilePath);
    }

    [Fact]
    public async Task SavedContentIsReadableFromDisk()
    {
        var info = await _service.SaveFileAsync(FormFile("мем.mp4", "содержимое"));

        var full = Path.Combine(
            _webRoot,
            info.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
        );
        Assert.Equal(
            "содержимое",
            await File.ReadAllTextAsync(full, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task HintIsUsedWhenGiven()
    {
        var info = await _service.SaveFileAsync(FormFile("мем.mp4", "x"), "Alerts/custom/имя.mp4");

        _createdPaths.Add(info.FilePath);
        Assert.Equal("/Alerts/custom/имя.mp4", info.FilePath);
    }

    [Fact]
    public async Task HintWithoutExtensionGetsSourceExtension()
    {
        var info = await _service.SaveFileAsync(
            FormFile("мем.mp4", "x"),
            "Alerts/custom/без-расширения"
        );

        // Расширение добавляется как отдельный сегмент: поведение зафиксировано,
        // чтобы его не приняли за опечатку при следующем прогоне.
        Assert.Equal("/Alerts/custom/без-расширения/mp4", info.FilePath);
    }

    [Theory]
    [InlineData("../../secret.mp4")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/system32.ini")]
    public async Task UnsafeHintIsRejected(string hint)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SaveFileAsync(FormFile("мем.mp4", "x"), hint)
        );
    }

    [Fact]
    public async Task FileIsDeleted()
    {
        var info = await _service.SaveFileAsync(FormFile("мем.mp4", "x"));
        var full = Path.Combine(
            _webRoot,
            info.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
        );

        await _service.DeleteFileAsync(info.FilePath);

        Assert.False(File.Exists(full));
    }

    [Fact]
    public async Task DeletingMissingFileDoesNothing()
    {
        await _service.DeleteFileAsync("/Alerts/uploaded_mems/нет-такого.mp4");
    }

    /// <summary>
    /// Путь с ведущим слешем приводится к относительному и остаётся внутри
    /// хранилища: абсолютный путь из БД не должен вывести удаление за пределы
    /// каталога.
    /// </summary>
    [Fact]
    public async Task AbsolutePathStaysInsideStorage()
    {
        await _service.DeleteFileAsync("/etc/passwd");

        Assert.False(
            File.Exists(Path.Combine(_webRoot, "etc", "passwd")) && !File.Exists("/etc/passwd")
        );
    }

    [Theory]
    [InlineData("../outside.mp4")]
    [InlineData("C:/Windows/system32.ini")]
    public async Task DeletingOutsideStorageIsRejected(string path)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteFileAsync(path));
    }

    /// <summary>
    /// Копия для разработки не обязана существовать: её отсутствие не должно
    /// ронять сохранение основного файла.
    /// </summary>
    [Fact]
    public async Task CopyToMissingSourceDoesNothing()
    {
        await _service.CopyToDevCopiesAsync("/Alerts/uploaded_mems/нет-такого.mp4");
    }

    /// <summary>
    /// Копия для разработки делается сразу при сохранении, и её недоступность не
    /// должна ронять сохранение: временный webroot вне проекта не имеет второй
    /// копии, и именно этот случай проверяется.
    /// </summary>
    [Fact]
    public async Task SavingSucceedsEvenWhenDevCopyIsImpossible()
    {
        var info = await _service.SaveFileAsync(FormFile("мем.mp4", "содержимое"));

        _createdPaths.Add(info.FilePath);
        Assert.True(
            File.Exists(
                Path.Combine(
                    _webRoot,
                    info.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                )
            )
        );
    }

    [Theory]
    [InlineData("../outside.mp4")]
    public async Task CopyOutsideStorageIsRejected(string path)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CopyToDevCopiesAsync(path)
        );
    }

    private static IFormFile FormFile(string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);

        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);
    }

    private sealed class StubEnvironment : IWebHostEnvironment
    {
        public StubEnvironment(string webRoot)
        {
            WebRootPath = webRoot;
            WebRootFileProvider = new PhysicalFileProvider(webRoot);
            ContentRootFileProvider = new PhysicalFileProvider(Path.GetTempPath());
        }

        public string WebRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; set; } = null!;

        public string ApplicationName { get; set; } = "MARS.MediaStorage.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = null!;

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public string EnvironmentName { get; set; } = "Test";
    }
}
