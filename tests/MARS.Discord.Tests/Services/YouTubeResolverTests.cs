using System.Reflection;
using MARS.Discord.Models;
using MARS.Discord.Services.YouTube;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Discord.Tests.Services;

/// <summary>
/// Поиск трека на YouTube для проигрывателя.
///
/// Резолвер ищет трек по названию, разбирает ссылку на видео и скачивает аудио для
/// очереди. Проверяется каждое из трёх: неверный трек означает, что в канале
/// зазвучит не то.
/// </summary>
public class YouTubeResolverTests
{
    private readonly FakeYouTubeApi _api = new();
    private readonly YouTubeResolver _resolver;

    public YouTubeResolverTests() => _resolver = new(_api, NullLogger<YouTubeResolver>.Instance);

    /// <summary>
    /// Найденные видео превращаются в треки с автором и обложкой: без них в
    /// описании трека было бы пустое название.
    /// </summary>
    [Fact]
    public async Task FoundVideosBecomeTracks()
    {
        _api.SearchResult =
        [
            Video("id1", "первый трек"),
            Video("id2", "второй трек", author: null, thumbnail: null),
        ];

        var tracks = await _resolver.SearchTracksAsync("трек", 5, Token);

        Assert.Equal(2, tracks.Length);
        Assert.Equal(["Автор"], tracks[0].Authors!);
        Assert.Equal("Автор - первый трек", tracks[0].Title);
        Assert.Empty(tracks[1].Authors ?? []);
        Assert.Equal(TimeSpan.FromMinutes(3), tracks[0].Duration);
    }

    /// <summary>
    /// Выдача ограничивается запрошенным количеством: команда «найди пять» не
    /// должна притаскивать весь плейлист.
    /// </summary>
    [Fact]
    public async Task SearchRespectsRequestedLimit()
    {
        _api.SearchResult = [Video("id1", "раз"), Video("id2", "два"), Video("id3", "три")];

        var tracks = await _resolver.SearchTracksAsync("трек", 2, Token);

        Assert.Equal(2, tracks.Length);
    }

    /// <summary>
    /// Пустой запрос не ходит в YouTube: иначе команда без названия тратила бы
    /// запрос и ждала ответа сети.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyQueryIsNotSearched(string query)
    {
        var tracks = await _resolver.SearchTracksAsync(query, 5, Token);

        Assert.Empty(tracks);
        Assert.Empty(_api.SearchQueries);
    }

    /// <summary>
    /// Сбой YouTube не выходит наружу: команда получает пустой результат, а не
    /// исключение в чате.
    /// </summary>
    [Fact]
    public async Task SearchFailureIsSwallowed()
    {
        _api.ThrowOnSearch = true;

        var tracks = await _resolver.SearchTracksAsync("трек", 5, Token);

        Assert.Empty(tracks);
    }

    /// <summary>
    /// Ссылка на видео разворачивается в трек: без этого команда с ссылкой не
    /// смогла бы добавить трек.
    /// </summary>
    [Fact]
    public async Task VideoUrlResolvesToTrack()
    {
        _api.VideoResult = Video("id9", "трек по ссылке");

        var track = await _resolver.ResolveVideoAsync("https://youtu.be/id9", Token);

        Assert.NotNull(track);
        Assert.Equal("id9", track.VideoId);
        Assert.Equal("трек по ссылке", track.TrackName);
    }

    /// <summary>
    /// Неизвестное видео не превращается в пустой трек: иначе в очередь попал бы
    /// файл без названия.
    /// </summary>
    [Fact]
    public async Task UnknownVideoResolvesToNull()
    {
        _api.VideoResult = null;

        var track = await _resolver.ResolveVideoAsync("https://youtu.be/nope", Token);

        Assert.Null(track);
    }

    /// <summary>
    /// Сбой при разборе ссылки не выходит наружу: команда сообщает «не найдено».
    /// </summary>
    [Fact]
    public async Task VideoFailureIsSwallowed()
    {
        _api.ThrowOnVideo = true;

        var track = await _resolver.ResolveVideoAsync("https://youtu.be/id9", Token);

        Assert.Null(track);
    }

    /// <summary>
    /// Идентификатор видео вытаскивается из любой формы ссылки: ссылки приходят от
    /// пользователей в разном виде.
    /// </summary>
    [Theory]
    [InlineData("https://youtu.be/abc123", "abc123")]
    [InlineData("https://www.youtube.com/watch?v=abc123&t=10", "abc123")]
    [InlineData("https://www.youtube.com/shorts/abc123", "abc123")]
    [InlineData("https://www.youtube.com/embed/abc123", "abc123")]
    [InlineData("https://example.com/abc123", null)]
    public void VideoIdIsExtractedFromUrl(string url, string? expected)
    {
        var track = new BaseTrackInfo { TrackName = "трек", Url = new Uri(url) };

        Assert.Equal(expected, _resolver.GetVideoId(track));
    }

    /// <summary>
    /// Готовый идентификатор важнее разбора ссылки: он уже известен и не должен
    /// перезаписываться.
    /// </summary>
    [Fact]
    public void KnownVideoIdWins()
    {
        var track = new BaseTrackInfo
        {
            TrackName = "трек",
            Url = new Uri("https://youtu.be/other"),
            VideoId = "known",
        };

        Assert.Equal("known", _resolver.GetVideoId(track));
    }

    /// <summary>
    /// Аудио скачивается в указанную папку, и путь возвращается: этот путь уходит в
    /// очередь озвучки.
    /// </summary>
    [Fact]
    public async Task AudioIsDownloadedIntoGivenDirectory()
    {
        _api.AudioStreams = [new YouTubeAudioStream("mp4", 128_000, true)];
        var directory = Path.Combine(Path.GetTempPath(), $"mars-yt-{Guid.NewGuid():N}");

        try
        {
            var path = await _resolver.DownloadBestAudioStreamAsync(
                Track("id42"),
                directory,
                Token
            );

            Assert.NotNull(path);
            Assert.StartsWith(directory, path);
            Assert.EndsWith(".m4a", path);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>
    /// Среди потоков выбирается лучший: иначе в очередь попал бы файл худшего
    /// качества.
    /// </summary>
    [Fact]
    public void BestAudioStreamIsChosen()
    {
        var streams = new[]
        {
            new YouTubeAudioStream("webm", 96_000, true),
            new YouTubeAudioStream("mp4", 192_000, true),
            new YouTubeAudioStream("mp4", 64_000, false),
        };

        var best = Invoke<YouTubeAudioStream?>("SelectBestStream", [streams]);

        Assert.Equal(192_000, best?.BitsPerSecond);
    }

    /// <summary>
    /// Если аудиопотоков нет, берётся muxed: иначе видео не прозаивалось бы вовсе.
    /// </summary>
    [Fact]
    public void MuxedStreamIsUsedWhenNoAudioOnlyStreamExists()
    {
        var streams = new[] { new YouTubeAudioStream("mp4", 64_000, false) };

        var best = Invoke<YouTubeAudioStream?>("SelectBestStream", [streams]);

        Assert.Equal(64_000, best?.BitsPerSecond);
    }

    /// <summary>
    /// Без трека или без папки скачивание не начинается: иначе сервис создал бы
    /// файлы неизвестно где.
    /// </summary>
    [Fact]
    public async Task DownloadIsSkippedWithoutTrackOrDirectory()
    {
        Assert.Null(await _resolver.DownloadBestAudioStreamAsync(null, "dir", Token));
        Assert.Null(await _resolver.DownloadBestAudioStreamAsync(Track("id1"), "  ", Token));
    }

    /// <summary>
    /// Скачивание без потоков не выдаёт путь на несуществующий файл: вызывающий
    /// код проверит наличие файла и сообщит об ошибке.
    /// </summary>
    [Fact]
    public async Task DownloadWithoutStreamsReturnsNoFakePath()
    {
        _api.AudioStreams = [];
        var directory = Path.Combine(Path.GetTempPath(), $"mars-yt-{Guid.NewGuid():N}");

        try
        {
            var path = await _resolver.DownloadBestAudioStreamAsync(
                Track("id77"),
                directory,
                Token
            );

            if (path is not null)
            {
                Assert.True(File.Exists(path), "Возвращён путь на несуществующий файл");
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>
    /// Ссылка на видео передаётся в скачивание: без неё yt-dlp скачал бы не то.
    /// </summary>
    [Fact]
    public void VideoUrlIsBuiltFromTrack()
    {
        Assert.Equal(
            "https://youtu.be/id5",
            Invoke<string>("BuildVideoUrl", [Track("id5"), "id5"])
        );
        Assert.Equal(
            "https://www.youtube.com/watch?v=id6",
            Invoke<string>("BuildVideoUrl", [Track(string.Empty), "id6"])
        );
    }

    /// <summary>
    /// Имя файла строится из названия без символов, запрещённых Windows, на любой
    /// ОС: <c>Path.GetInvalidFileNameChars</c> на Linux знает только про NUL и
    /// «/», а файл с кавычкой или «?» не открывается на Windows. Поэтому
    /// проверяется весь запрещённый набор, а не только текущий.
    /// </summary>
    [Fact]
    public void SafeFileNameDropsInvalidCharacters()
    {
        var name = Invoke<string>("BuildSafeFileName", ["трек / \"кавычки\"", "fallback"]);

        Assert.Equal("трек _ _кавычки_", name);
        Assert.DoesNotContain('/', name);
        Assert.DoesNotContain('\\', name);
        Assert.DoesNotContain('"', name);
        Assert.DoesNotContain(':', name);
        Assert.DoesNotContain('*', name);
        Assert.DoesNotContain('?', name);
        Assert.DoesNotContain('<', name);
        Assert.DoesNotContain('>', name);
        Assert.DoesNotContain('|', name);
    }

    /// <summary>
    /// Без названия используется идентификатор видео: иначе файл был бы без имени.
    /// </summary>
    [Fact]
    public void SafeFileNameFallsBackToVideoId()
    {
        Assert.Equal("fallback", Invoke<string>("BuildSafeFileName", ["   ", "fallback"]));
    }

    /// <summary>
    /// Скачанный файл выбирается самый свежий, а временные расширения
    /// игнорируются: иначе в очередь попал бы недокачанный файл.
    /// </summary>
    [Fact]
    public void DownloadedFileIsNewestCompleteOne()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mars-yt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var older = Write(directory, "трек.m4a", 3);
        var newer = Write(directory, "трек.webm", 1);
        var partial = Write(directory, "трек.m4a.part", 2);

        try
        {
            var found = Invoke<string?>(
                "FindDownloadedFile",
                [directory, "трек", new HashSet<string> { older, newer, partial }]
            );

            Assert.Equal(newer, found);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Среди готовых файлов выбирается самый свежий — только когда новых нет: иначе
    /// повторная загрузка того же трека не обновляла бы файл.
    /// </summary>
    [Fact]
    public void ExistingFileIsUsedWhenNothingNewArrived()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mars-yt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var existing = Write(directory, "трек.m4a", 1);

        try
        {
            var found = Invoke<string?>(
                "FindDownloadedFile",
                [directory, "трек", new HashSet<string> { existing }]
            );

            Assert.Equal(existing, found);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Расширение аудио в mp4 становится m4a: расширение должно совпадать с
    /// содержимым, иначе плеер не отдаст файл.
    /// </summary>
    [Fact]
    public void M4aExtensionIsUsedForMp4Audio()
    {
        Assert.Equal(
            "m4a",
            Invoke<string>("GetStreamExtension", [new YouTubeAudioStream("mp4", 128_000, true)])
        );
        Assert.Equal(
            "webm",
            Invoke<string>("GetStreamExtension", [new YouTubeAudioStream("webm", 128_000, true)])
        );
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static YouTubeVideoInfo Video(
        string id,
        string title,
        string? author = "Автор",
        string? thumbnail = "https://example.com/thumb.jpg"
    ) => new(id, $"https://youtu.be/{id}", title, author, TimeSpan.FromMinutes(3), thumbnail);

    private static BaseTrackInfo Track(string videoId) =>
        new()
        {
            TrackName = "трек для скачивания",
            Url = string.IsNullOrWhiteSpace(videoId)
                ? null!
                : new Uri($"https://youtu.be/{videoId}"),
        };

    private static string Write(string directory, string name, int minutesAgo)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, name);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-minutesAgo));
        return path;
    }

    private T Invoke<T>(string name, object?[] arguments)
    {
        var method = typeof(YouTubeResolver).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (T)method.Invoke(null, arguments)!;
    }

    /// <summary>
    /// Заглушка YouTube: настоящий клиент ходит в сеть и только что не загружался
    /// в тестовом хосте, поэтому проверяется логика резолвера, а не HTTP-запросы.
    /// </summary>
    private sealed class FakeYouTubeApi : IYouTubeApi
    {
        public IReadOnlyList<YouTubeVideoInfo> SearchResult { get; set; } = [];
        public IReadOnlyList<YouTubeAudioStream> AudioStreams { get; set; } = [];
        public YouTubeVideoInfo? VideoResult { get; set; }
        public List<string> SearchQueries { get; } = [];
        public bool ThrowOnSearch { get; set; }
        public bool ThrowOnVideo { get; set; }

        public Task<IReadOnlyList<YouTubeVideoInfo>> SearchVideosAsync(
            string query,
            int maxResults,
            CancellationToken cancellationToken = default
        )
        {
            SearchQueries.Add(query);

            if (ThrowOnSearch)
            {
                throw new InvalidOperationException("YouTube недоступен");
            }

            return Task.FromResult<IReadOnlyList<YouTubeVideoInfo>>(
                SearchResult.Take(maxResults).ToArray()
            );
        }

        public Task<YouTubeVideoInfo?> GetVideoAsync(
            string videoUrlOrId,
            CancellationToken cancellationToken = default
        ) =>
            ThrowOnVideo
                ? throw new InvalidOperationException("YouTube недоступен")
                : Task.FromResult(VideoResult);

        public Task<IReadOnlyList<YouTubeAudioStream>> GetAudioStreamsAsync(
            string videoId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(AudioStreams);

        public Task DownloadAsync(
            string videoId,
            YouTubeAudioStream stream,
            string filePath,
            CancellationToken cancellationToken = default
        )
        {
            // Настоящий клиент пишет файл на диск, и проверка должна увидеть
            // результат так же, как его увидит плеер.
            File.WriteAllBytes(filePath, [1, 2, 3]);
            return Task.CompletedTask;
        }
    }
}
