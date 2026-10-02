using System.Reflection;
using MARS.MediaStorage.Services.Media;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Перекодирование видео для Chrome.
///
/// Низкий битрейт — единственная причина перекодировать, и проверяется именно
/// она: лишнее перекодирование портит картинку и тратит время, а пропущенное
/// видео в OBS не играет вообще.
///
/// Сам ffmpeg не проверяется: на машине разработчика он может стоять, а на
/// CI-раннере — нет, и результат прыгал бы между прогонами. Все проверки идут по
/// пути, где ffmpeg не нужен: кэш, отказ и отказоустойчивые ветки.
/// </summary>
public class MediaTranscoderTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mars-transcoder-tests",
        Guid.NewGuid().ToString("N")
    );

    private readonly Mock<IMediaInspector> _inspector = new();
    private readonly MediaTranscoder _transcoder = new(
        new StubEnvironment(""),
        NullLogger<MediaTranscoder>.Instance,
        Mock.Of<IMediaInspector>()
    );

    public MediaTranscoderTests()
    {
        Directory.CreateDirectory(_root);
        _transcoder = new MediaTranscoder(
            new StubEnvironment(_root),
            NullLogger<MediaTranscoder>.Instance,
            _inspector.Object
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"\u0000")]
    public async Task MissingSourceIsReturnedAsIs(string sourcePath)
    {
        var path = sourcePath == "\u0000" ? Path.Combine(_root, "нет-такого.mp4") : sourcePath;

        Assert.Equal(
            path,
            await _transcoder.EnsurePlayableAsync(path, TestContext.Current.CancellationToken)
        );
    }

    /// <summary>
    /// Аудио и картинки не перекодируются: браузер и OBS их играют как есть, а
    /// перекодирование сломало бы поток.
    /// </summary>
    [Theory]
    [InlineData("track.mp3")]
    [InlineData("frame.png")]
    public async Task NonVideoIsReturnedAsIs(string fileName)
    {
        var source = WriteSource(fileName, bitrateKbps: 64);

        var result = await _transcoder.EnsurePlayableAsync(
            source,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(source, result);
    }

    /// <summary>
    /// Видео с достаточным битрейтом остаётся как есть: перекодирование ради
    /// перекодирования портит качество.
    /// </summary>
    [Fact]
    public async Task VideoWithGoodBitrateIsReturnedAsIs()
    {
        var source = WriteSource("good.mp4", bitrateKbps: 1000);

        var result = await _transcoder.EnsurePlayableAsync(
            source,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(source, result);
    }

    /// <summary>
    /// Готовый кэш отдаётся без перекодирования: он и существует ради этого,
    /// а время перекодирования ушло бы на каждый просмотр заново.
    /// </summary>
    [Fact]
    public async Task CachedConversionIsReused()
    {
        var source = WriteSource("cached.mp4", bitrateKbps: 64);
        var cachePath = CachePathOf(source);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        File.WriteAllText(cachePath, "перекодировано");
        File.SetLastWriteTimeUtc(cachePath, File.GetLastWriteTimeUtc(source));

        var result = await _transcoder.EnsurePlayableAsync(
            source,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(cachePath, result);
    }

    /// <summary>
    /// Устаревший кэш не отдаётся зрителю: он сделан для другой версии исходника.
    /// Проверяется содержимое, а не путь: без ffmpeg метод вернёт исходник, а с
    /// ffmpeg — свежий файл по тому же пути, и проверка пути зависела бы от того,
    /// стоит ли ffmpeg на машине.
    /// </summary>
    [Fact]
    public async Task OutdatedCacheIsNotReused()
    {
        var source = WriteSource("outdated.mp4", bitrateKbps: 64);
        var cachePath = CachePathOf(source);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        File.WriteAllText(cachePath, "старый");
        File.SetLastWriteTimeUtc(cachePath, DateTime.UtcNow.AddDays(-3));

        var result = await _transcoder.EnsurePlayableAsync(
            source,
            TestContext.Current.CancellationToken
        );

        Assert.NotEqual("старый", await ReadAsync(result));
    }

    private static async Task<string> ReadAsync(string path) =>
        File.Exists(path) ? await File.ReadAllTextAsync(path) : string.Empty;

    /// <summary>
    /// Неисправный ffprobe не должен ронять выдачу файла: лучше отдать исходник и
    /// показать его как есть, чем ничего не отдать.
    /// </summary>
    [Fact]
    public async Task InspectorFailureFallsBackToSource()
    {
        var source = WriteSource("broken.mp4", bitrateKbps: 64);
        _inspector
            .Setup(instance =>
                instance.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("ffprobe не найден"));

        Assert.Equal(
            source,
            await _transcoder.EnsurePlayableAsync(source, TestContext.Current.CancellationToken)
        );
    }

    /// <summary>
    /// Текст в лог содержит имя файла и битрейт: без них в журнале не понять,
    /// что именно перекодировалось и почему.
    /// </summary>
    [Fact]
    public void ConversionMessageNamesFileAndBitrate()
    {
        var message = (string)InvokeStatic(
            "GetConversionMessage",
            "clip.mp4",
            MediaType.Video,
            (long)512
        );

        Assert.Contains("clip.mp4", message);
        Assert.Contains("512 kbps", message);
    }

    [Fact]
    public void ConversionMessageSurvivesUnknownBitrate()
    {
        long? unknownBitrate = null;
        var message = (string)InvokeStatic(
            "GetConversionMessage",
            "clip.mp4",
            MediaType.Video,
            unknownBitrate!
        );

        Assert.Contains("clip.mp4", message);
        Assert.Contains("unknown", message);
    }

    private string WriteSource(string fileName, long bitrateKbps)
    {
        var path = Path.Combine(_root, fileName);
        File.WriteAllText(path, "исходник");
        _inspector
            .Setup(instance => instance.ProbeAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaProbeResult(BitrateKbps: bitrateKbps));

        return path;
    }

    private string CachePathOf(string source) =>
        MediaTranscodePathPolicy.GetTranscodedCachePath(
            source,
            Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(source))!,
                MediaTranscodePathPolicy.ConvertedFolderName
            )
        );

    /// <summary>
    /// Текст сообщения приватный и вызывается только при перекодировании, которое
    /// здесь не запускается: проверяется он напрямую.
    /// </summary>
    private static object InvokeStatic(string method, params object[] arguments) =>
        typeof(MediaTranscoder)
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, arguments)!;

    private sealed class StubEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ApplicationName { get; set; } = "MARS.MediaStorage.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = webRoot;

        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
