using System.Reflection;
using MARS.MediaStorage.Controllers;
using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.MediaStorage.Tests.Controllers;

/// <summary>
/// Внутренние проверки путей и правил в контроллере алертов.
///
/// Здесь решается, чей файл отдаётся наружу: путь из запроса не должен выводить
/// контроллер за пределы хранилища. Проверки приватные, поэтому вызываются так же,
/// как их зовёт сам контроллер.
/// </summary>
public class MediaInfoApiControllerPathTests : IDisposable
{
    private readonly string _webRoot = Path.Combine(
        Path.GetTempPath(),
        "mars-media-api",
        Guid.NewGuid().ToString("N")
    );

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, true);
        }
    }

    /// <summary>
    /// Относительный путь разворачивается внутрь wwwroot, а не относительно текущего
    /// каталога процесса: иначе файл не находился бы при запуске из другой папки.
    /// </summary>
    [Fact]
    public void MediaPathIsResolvedUnderWebRoot()
    {
        var resolved = Call<string>("ResolveMediaPath", Create(), "Alerts/random_meme/a.mp3");

        Assert.StartsWith(Path.GetFullPath(_webRoot), resolved);
        Assert.EndsWith("a.mp3", resolved);
    }

    /// <summary>
    /// Обратное преобразование отдаёт путь хранилища с ведущим «/»: без него ссылка
    /// на файл не открывалась бы в браузере.
    /// </summary>
    [Fact]
    public void StorageUrlStartsWithSlash()
    {
        var url = Call<string>(
            "ToStorageUrl",
            Create(),
            Path.Combine(_webRoot, "Alerts", "random_meme", "a.mp3")
        );

        Assert.Equal("/Alerts/random_meme/a.mp3", url);
    }

    [Fact]
    public void UploadedMemsPathIsAccepted()
    {
        var (ok, resolved, error) = MemsPath("/Alerts/uploaded_mems/a.mp3");

        Assert.True(ok);
        Assert.Equal("/Alerts/uploaded_mems/a.mp3", resolved);
        Assert.Empty(error);
    }

    /// <summary>
    /// Путь с выходом за пределы хранилища отвергается: иначе через загрузку мема
    /// можно было бы прочитать любой файл контейнера.
    /// </summary>
    [Theory]
    [InlineData("/Alerts/uploaded_mems/../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/system.ini")]
    public void UnsafePathIsRejected(string path)
    {
        var (ok, _, error) = MemsPath(path);

        Assert.False(ok);
        Assert.NotEmpty(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPathIsRejected(string? path)
    {
        var (ok, _, error) = MemsPath(path);

        Assert.False(ok);
        Assert.NotEmpty(error);
    }

    /// <summary>
    /// Папка без имени файла бесполезна: скачивать нечего.
    /// </summary>
    [Fact]
    public void FolderWithoutFileNameIsRejected()
    {
        var (ok, _, error) = MemsPath("/Alerts/uploaded_mems/");

        Assert.False(ok);
        Assert.NotEmpty(error);
    }

    /// <summary>
    /// Разделители приводятся к одному виду: на Linux обратный слеш стал бы частью
    /// имени файла, и путь перестал бы существовать.
    /// </summary>
    [Fact]
    public void RelativePathIsNormalized()
    {
        var normalized = Call<string>(
            "NormalizeRelativePath",
            null,
            @"Alerts\uploaded_mems\a.mp3",
            isStatic: true
        );

        Assert.Equal("/Alerts/uploaded_mems/a.mp3", normalized);
    }

    [Fact]
    public void DoubleSlashesCollapse()
    {
        var normalized = Call<string>(
            "NormalizeRelativePath",
            null,
            "//Alerts///a.mp3",
            isStatic: true
        );

        Assert.Equal("/Alerts/a.mp3", normalized);
    }

    /// <summary>
    /// Заморозка разрешена только для высокого приоритета: иначе эффект заморозки
    /// показывался бы обычным оповещением и пугал зрителя.
    /// </summary>
    [Fact]
    public void FreezeRequiresHighPriority()
    {
        var valid = Call<bool>(
            "IsFreezeRuleValid",
            null,
            Meta(freeze: true, priority: MediaAlertPriority.High),
            isStatic: true
        );
        var invalid = Call<bool>(
            "IsFreezeRuleValid",
            null,
            Meta(freeze: true, priority: MediaAlertPriority.Normal),
            isStatic: true
        );
        var withoutFreeze = Call<bool>(
            "IsFreezeRuleValid",
            null,
            Meta(freeze: false, priority: MediaAlertPriority.Normal),
            isStatic: true
        );

        Assert.True(valid);
        Assert.False(invalid);
        Assert.True(withoutFreeze);
    }

    /// <summary>
    /// При сохранении записи подставляется нормализованный файл, а остальные поля
    /// берутся из запроса: иначе алерт ссылался бы на исходный файл до конвертации.
    /// </summary>
    [Fact]
    public void StoredAlertUsesConvertedFile()
    {
        var source = new ApiMediaInfo
        {
            TextInfo = new MediaTextInfo { Text = "текст" },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Audio,
                FilePath = "memory/a.mp3",
                FileName = "a",
                Extension = ".mp3",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "аяка" },
            StylesInfo = new MediaStylesInfo(),
        };
        var converted = new MediaFileInfo
        {
            Type = MediaType.Audio,
            FilePath = "/Alerts/a.opus",
            FileName = "a.opus",
            Extension = ".opus",
        };

        var stored = Call<MediaInfo>("CreateStoredAlert", null, source, converted, isStatic: true);

        Assert.Equal("/Alerts/a.opus", stored.FileInfo.FilePath);
        Assert.Equal("аяка", stored.MetaInfo.DisplayName);
        Assert.Equal(source.Id, stored.Id);
    }

    private static MediaMetaInfo Meta(bool freeze, MediaAlertPriority priority) =>
        new()
        {
            DisplayName = "аяка",
            IsFreezeRequired = freeze,
            Priority = priority,
        };

    private MediaInfoApiController Create() =>
        new(
            Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<MARS.MediaStorage.DataBaseContext.MediaStorageDbContext>>(),
            NullLogger<MediaInfoApiController>.Instance,
            Mock.Of<IMediaFileStorageService>(),
            Mock.Of<IMediaStorageService>(),
            Mock.Of<IMediaInspector>(),
            Mock.Of<IMediaTranscoder>(),
            new FakeEnvironment(_webRoot)
        );

    /// <summary>
    /// <summary>
    /// Приватные проверки вызываются так же, как из контроллера: имя метода и
    /// аргументы — единственный контракт между ними.
    /// </summary>
    private static T Call<T>(
        string name,
        MediaInfoApiController? instance,
        object? first,
        object? second = null,
        bool isStatic = false
    )
    {
        var method = typeof(MediaInfoApiController).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic
        )!;

        object?[] arguments = second is null ? [first] : [first, second];

        return (T)method.Invoke(isStatic ? null : instance, arguments)!;
    }

    /// <summary>Проверка пути загружаемого мема с её двумя выходными значениями.</summary>
    private static (bool Ok, string Resolved, string Error) MemsPath(string? path)
    {
        var method = typeof(MediaInfoApiController).GetMethod(
            "TryResolveUploadedMemsFilePath",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        var arguments = new object?[] { path, null, null };
        var ok = (bool)method.Invoke(null, arguments)!;

        return (ok, (string)arguments[1]!, (string)arguments[2]!);
    }

    private sealed class FakeEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MARS.MediaStorage.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Test";
    }
}
