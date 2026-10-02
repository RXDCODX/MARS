using System.Reflection;
using MARS.Discord.Models;
using MARS.Discord.Services.PlayRequest;
using MARS.Discord.Services.YouTube;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Discord.Tests.Services;

/// <summary>
/// Подготовка аудио для отправки в голосовой канал Discord.
///
/// Discord ограничивает размер вложения, поэтому трек приходится сжимать или
/// отдавать в исходном контейнере. Проверяются три решения, из-за которых класс и
/// написан: файл из кэша берётся без повторной обработки, трек без video id
/// отклоняется сразу, а не скачивается, и невмещающийся файл честно сообщает об
/// ошибке вместо битого файла.
///
/// Конвертация ffmpeg намеренно не проверяется: на машине разработчика ffmpeg
/// может стоять, а на CI-раннере — нет, и результат прыгал бы между прогонами.
/// Все проверки идут до конвертации.
/// </summary>
public class DiscordPlayAudioCacheServiceTests : IDisposable
{
    private const int MaxSize = (int)DiscordPlayAudioCacheService.DefaultMaxAttachmentSizeBytes;

    private static readonly string CacheDirectory = Path.Combine(
        Path.GetTempPath(),
        "mars-discord-play-cache"
    );

    private readonly Mock<IYouTubeResolver> _resolver = new();
    private readonly List<string> _createdFiles = [];
    private readonly DiscordPlayAudioCacheService _service = new(
        Mock.Of<IYouTubeResolver>(),
        NullLogger<DiscordPlayAudioCacheService>.Instance
    );

    public DiscordPlayAudioCacheServiceTests()
    {
        _service = new DiscordPlayAudioCacheService(
            _resolver.Object,
            NullLogger<DiscordPlayAudioCacheService>.Instance
        );
    }

    public void Dispose()
    {
        foreach (var path in _createdFiles)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Трек без video id не отправляется в сеть: ссылку не с чем разбирать, и
    /// пользователю честнее сказать об этом, чем ждать таймаут.
    /// </summary>
    [Fact]
    public async Task TrackWithoutVideoIdIsRejected()
    {
        _resolver
            .Setup(instance => instance.GetVideoId(It.IsAny<BaseTrackInfo>()))
            .Returns((string?)null);

        var result = await _service.PrepareAudioAsync(
            Track(),
            MaxSize,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.Contains("video id", result.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveSizeIsRejected(long size)
    {
        var result = await _service.PrepareAudioAsync(
            Track(),
            size,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.Contains("Неверные параметры", result.Message);
    }

    /// <summary>
    /// Файл из кэша отдаётся без обращения к YouTube: повторная загрузка того же
    /// трека съедала бы и трафик, и лимиты Discord.
    /// </summary>
    [Fact]
    public async Task CachedFileIsUsedWithoutDownload()
    {
        var videoId = NewVideoId();
        WriteCachedFile(videoId, "128kbps.mp3", 1024);
        _resolver
            .Setup(instance => instance.GetVideoId(It.IsAny<BaseTrackInfo>()))
            .Returns(videoId);

        var result = await _service.PrepareAudioAsync(
            Track(videoId),
            MaxSize,
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Success);
        Assert.True(result.Data.IsFromCache);
        Assert.Equal(128, result.Data.BitrateKbps);
        _resolver.Verify(
            instance =>
                instance.DownloadBestAudioStreamAsync(
                    It.IsAny<BaseTrackInfo>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// Кэш старше недели не используется: качество исходника могло уехать, а
    /// файл всё равно занимает место на диске.
    /// </summary>
    [Fact]
    public async Task ExpiredCacheFileIsNotUsed()
    {
        var videoId = NewVideoId();
        var path = WriteCachedFile(videoId, "96kbps.mp3", 1024);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-8));
        _resolver
            .Setup(instance => instance.GetVideoId(It.IsAny<BaseTrackInfo>()))
            .Returns(videoId);
        _resolver
            .Setup(instance =>
                instance.DownloadBestAudioStreamAsync(
                    It.IsAny<BaseTrackInfo>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string?)null);

        var result = await _service.PrepareAudioAsync(
            Track(videoId),
            MaxSize,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.Contains("Не удалось скачать", result.Message);
        Assert.False(File.Exists(path));
    }

    /// <summary>
    /// Кэш больше лимита Discord игнорируется: такой файл всё равно не
    /// отправить, и ошибка должна быть про сжатие, а не про кэш.
    /// </summary>
    [Fact]
    public async Task OversizedCacheFileIsIgnored()
    {
        var videoId = NewVideoId();
        WriteCachedFile(videoId, "128kbps.mp3", MaxSize + 1);
        _resolver
            .Setup(instance => instance.GetVideoId(It.IsAny<BaseTrackInfo>()))
            .Returns(videoId);
        _resolver
            .Setup(instance =>
                instance.DownloadBestAudioStreamAsync(
                    It.IsAny<BaseTrackInfo>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string?)null);

        var result = await _service.PrepareAudioAsync(
            Track(videoId),
            MaxSize,
            TestContext.Current.CancellationToken
        );

        Assert.Contains("Не удалось скачать", result.Message);
    }

    /// <summary>
    /// Неудачное скачивание — это «не получилось», а не исключение наружу: команда
    /// в чате должна получить внятный отказ.
    /// </summary>
    [Fact]
    public async Task FailedDownloadIsReported()
    {
        var videoId = NewVideoId();
        _resolver
            .Setup(instance => instance.GetVideoId(It.IsAny<BaseTrackInfo>()))
            .Returns(videoId);
        _resolver
            .Setup(instance =>
                instance.DownloadBestAudioStreamAsync(
                    It.IsAny<BaseTrackInfo>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string?)null);

        var result = await _service.PrepareAudioAsync(
            Track(videoId),
            MaxSize,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DownloadFailureIsCaught()
    {
        var videoId = NewVideoId();
        _resolver
            .Setup(instance => instance.GetVideoId(It.IsAny<BaseTrackInfo>()))
            .Returns(videoId);
        _resolver
            .Setup(instance =>
                instance.DownloadBestAudioStreamAsync(
                    It.IsAny<BaseTrackInfo>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new IOException("yt-dlp не найден"));

        var result = await _service.PrepareAudioAsync(
            Track(videoId),
            MaxSize,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Success);
        Assert.Contains("Ошибка подготовки аудио", result.Message);
    }

    /// <summary>
    /// Полчаса аудио в лимит Discord 10 MB не влезает на 192 кбит/с: битрейт
    /// опускается, иначе отправка сорвалась бы на размере вложения.
    /// </summary>
    [Fact]
    public void LongTracksGetLowerBitrate()
    {
        var bitrates =
            (IReadOnlyList<int>)
                InvokeStatic("BuildBitrateCandidates", TimeSpan.FromMinutes(30), MaxSize);

        Assert.DoesNotContain(192, bitrates);
        Assert.Contains(bitrates, bitrate => bitrate > 0);
    }

    [Fact]
    public void ShortTracksKeepHighBitrate()
    {
        var bitrates =
            (IReadOnlyList<int>)
                InvokeStatic("BuildBitrateCandidates", TimeSpan.FromSeconds(30), MaxSize);

        Assert.Equal([192, 160, 128, 96, 80, 64, 48, 32], bitrates);
    }

    /// <summary>
    /// Даже при невозможных входных данных остаётся хоть один кандидат: пустой
    /// список означал бы «перебор не выполнился» и отказ вместо подготовки.
    /// </summary>
    [Fact]
    public void BitrateListIsNeverEmpty()
    {
        var bitrates = (IReadOnlyList<int>)InvokeStatic("BuildBitrateCandidates", TimeSpan.Zero, 1);

        Assert.NotEmpty(bitrates);
    }

    [Theory]
    [InlineData("video_128kbps.mp3", 128)]
    [InlineData("video_96kbps.MP3", 96)]
    [InlineData("video_source.webm", 0)]
    public void BitrateIsTakenFromFileName(string fileName, int expected)
    {
        Assert.Equal(expected, (int)InvokeStatic("ExtractBitrateFromFileName", fileName));
    }

    /// <summary>
    /// Размер файла в сообщении об ошибке печатается в привычных единицах, а не в
    /// байтах: «не помещается в лимит Discord 10 MB» читается, «10485760 байт» —
    /// нет.
    /// </summary>
    [Theory]
    [InlineData(5L * 1024 * 1024, "5 MB")]
    [InlineData(2048L, "2 KB")]
    [InlineData(512L, "512 B")]
    public void SizeIsFormattedForUser(long bytes, string expected)
    {
        Assert.Equal(expected, (string)InvokeStatic("FormatFileSize", bytes));
    }

    private static BaseTrackInfo Track(string? videoId = "video-1") =>
        new()
        {
            TrackName = "трек",
            Url = new Uri("https://www.youtube.com/watch?v=" + (videoId ?? "нет")),
            VideoId = videoId,
            Duration = TimeSpan.FromMinutes(1),
        };

    private static string NewVideoId() => "video-" + Guid.NewGuid().ToString("N");

    private string WriteCachedFile(string videoId, string suffix, long size)
    {
        Directory.CreateDirectory(CacheDirectory);
        var path = Path.Combine(CacheDirectory, videoId + "_" + suffix);
        File.WriteAllBytes(path, new byte[size]);
        _createdFiles.Add(path);

        return path;
    }

    private static object InvokeStatic(string method, params object[] arguments) =>
        typeof(DiscordPlayAudioCacheService)
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, arguments)!;
}
