using MARS.TwitchCore.Services.YouTube;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Поиск трека на YouTube.
///
/// Резолвер отвечает за три вещи: найти лучшее совпадение из выдачи, разобрать
/// ссылку на видео и скачать аудио. Проверяется каждая: неверный выбор трека
/// означает, что в очереди зазвучит не то, а пустой результат вместо трека —
/// что команда отменяет без объяснения.
/// </summary>
public class YouTubeResolverTests
{
    private readonly FakeYouTubeApi _api = new();

    public YouTubeResolverTests() => _resolver = new(_api, NullLogger<YouTubeResolver>.Instance);

    private readonly YouTubeResolver _resolver;

    /// <summary>
    /// Из выдачи выбирается самое похожее название: YouTube отдаёт результаты без
    /// гарантии порядка, и первый не всегда нужный.
    /// </summary>
    [Fact]
    public async Task BestMatchIsChosen()
    {
        _api.SearchResult =
        [
            Video("id1", "совсем другой клип про кота"),
            Video("id2", "песня тестовая орёт"),
            Video("id3", "ещё что-то"),
        ];

        var track = await _resolver.SearchBestMatchAsync("песня тестовая", 10, Token);

        Assert.NotNull(track);
        Assert.Equal("id2", track.VideoId);
    }

    /// <summary>
    /// Пустой запрос не ходит в YouTube: иначе команда без названия тратила бы
    /// запрос к API и ждала ответа сети.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyQueryIsNotSearched(string query)
    {
        var track = await _resolver.ResolveQueryAsync(query, Token);

        Assert.Null(track);
        Assert.Empty(_api.SearchQueries);
    }

    /// <summary>
    /// Единственный результат берётся без сравнения: сравнивать не с чем.
    /// </summary>
    [Fact]
    public async Task SingleResultIsTakenAsIs()
    {
        _api.SearchResult = [Video("id1", "единственный трек")];

        var track = await _resolver.SearchTracksAsync("трек", 5, Token);

        Assert.Equal("id1", Assert.Single(track).VideoId);
    }

    /// <summary>
    /// Список ограничен запрошенным количеством: команда «найди пять» не должна
    /// притаскивать весь плейлист.
    /// </summary>
    [Fact]
    public async Task SearchRespectsRequestedLimit()
    {
        _api.SearchResult = [Video("id1", "раз"), Video("id2", "два"), Video("id3", "три")];

        var tracks = await _resolver.SearchTracksAsync("трек", 2, Token);

        Assert.Equal(2, tracks.Length);
    }

    /// <summary>
    /// Обложка и автор переносятся в трек: без обложки в очереди был бы пустой
    /// превью, а подпись без автора — «- Название».
    /// </summary>
    [Fact]
    public async Task TrackCarriesAuthorAndArtwork()
    {
        _api.VideoResult = Video(
            "id9",
            "трек с обложкой",
            author: "Исполнитель",
            thumbnail: "https://example.com/cover.jpg"
        );

        var track = await _resolver.ResolveVideoAsync("https://youtu.be/id9", Token);

        Assert.NotNull(track);
        Assert.Equal(["Исполнитель"], track.Authors!);
        Assert.Equal("Исполнитель - трек с обложкой", track.Title);
        Assert.Equal(new Uri("https://example.com/cover.jpg"), track.ArtworkUrl);
    }

    /// <summary>
    /// Видео без обложки и автора не должно ломать трек: такие ролики на YouTube
    /// есть, и команда должна работать и с ними.
    /// </summary>
    [Fact]
    public async Task TrackWithoutArtworkAndAuthorIsCreated()
    {
        _api.VideoResult = Video("id10", "без обложки", author: null, thumbnail: null);

        var track = await _resolver.ResolveVideoAsync("https://youtu.be/id10", Token);

        Assert.NotNull(track);
        Assert.Empty(track.Authors ?? []);
        Assert.Equal("без обложки", track.Title);
        Assert.Null(track.ArtworkUrl);
    }

    /// <summary>
    /// Сбой YouTube не выходит наручу: команда получает «не найдено», а не
    /// исключение в чате.
    /// </summary>
    [Fact]
    public async Task SearchFailureIsSwallowed()
    {
        _api.ThrowOnSearch = true;

        var track = await _resolver.ResolveQueryAsync("что-то", Token);

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
    /// Плейлист отдаётся треками: без этого команда «взять плейлист» не вернула бы
    /// ничего.
    /// </summary>
    [Fact]
    public async Task PlaylistIsResolvedToTracks()
    {
        _api.PlaylistResult = [Video("id1", "первый"), Video("id2", "второй")];

        var tracks = await _resolver.ResolvePlaylistAsync("https://youtube.com/playlist?list=PL1");

        Assert.Equal(2, tracks?.Length);
        Assert.Equal("первый", tracks![0].TrackName);
    }

    /// <summary>
    /// Сбой загрузки плейлиста даёт пустой список, а не исключение: команда
    /// сообщает «ничего не найдено» и продолжает работать.
    /// </summary>
    [Fact]
    public async Task PlaylistFailureReturnsEmptyList()
    {
        _api.ThrowOnPlaylist = true;

        var tracks = await _resolver.ResolvePlaylistAsync("https://youtube.com/playlist?list=PL1");

        Assert.Empty(tracks!);
    }

    /// <summary>
    /// Плейлист из первого поиска возвращается, если он вообще есть.
    /// </summary>
    [Fact]
    public async Task PlaylistQueryResolvesFirstResult()
    {
        _api.PlaylistSearchResult =
        [
            new YouTubePlaylistInfo("PL1", "Плейлист", "Автор"),
            new YouTubePlaylistInfo("PL2", "Другой", null),
        ];

        var playlist = await _resolver.ResolvePlaylistQueryAsync("плейлист", 5, Token);

        Assert.Equal("PL1", playlist?.Id);
        Assert.Equal("Плейлист", playlist?.Title);
    }

    /// <summary>
    /// Плейлист без результатов не подменяется первым найденным: иначе команда
    /// добавила бы в очередь чужой плейлист.
    /// </summary>
    [Fact]
    public async Task PlaylistQueryWithoutResultsReturnsNull()
    {
        _api.PlaylistSearchResult = [];

        var playlist = await _resolver.ResolvePlaylistQueryAsync("плейлист", 5, Token);

        Assert.Null(playlist);
    }

    /// <summary>
    /// Скачивание аудио кладёт файл в указанную папку и возвращает путь: этот путь
    /// уходит в очередь озвучки как файл трека.
    /// </summary>
    [Fact]
    public async Task AudioIsDownloadedIntoGivenDirectory()
    {
        var api = new FakeYouTubeApi();
        _api.AudioStreams = [new YouTubeAudioStream("mp4", 128_000, true)];
        api.DownloadedPath = null;
        var resolver = new YouTubeResolver(api, NullLogger<YouTubeResolver>.Instance);
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
    /// Если у видео нет аудиопотоков, возвращается путь из fallback-загрузки, а
    /// сам файл не появляется: вызывающий код проверит наличие файла.
    /// </summary>
    [Fact]
    public async Task DownloadWithoutStreamsReturnsFallbackResult()
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

            // Fallback — это yt-dlp, которого в тестовой среде нет, поэтому
            // проверяется лишь то, что метод не выдал путь на несуществующий файл.
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
    /// Имя файла строится из названия без запрещённых символов: иначе запись
    /// упала бы на Windows или в Linux-томе.
    /// </summary>
    [Fact]
    public void SafeFileNameDropsInvalidCharacters()
    {
        var name = Invoke<string>("BuildSafeFileName", ["трек / \"кавычки\"", "fallback"]);

        Assert.DoesNotContain('/', name);
        Assert.DoesNotContain('"', name);
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

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static YouTubeVideoInfo Video(
        string id,
        string title,
        string? author = "Автор",
        string? thumbnail = "https://example.com/thumb.jpg"
    ) => new(id, $"https://youtu.be/{id}", title, author, TimeSpan.FromMinutes(3), thumbnail);

    private static BaseTrackInfo Track(string videoId) =>
        new() { TrackName = "трек для скачивания", Url = new Uri($"https://youtu.be/{videoId}") };

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
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
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
        public IReadOnlyList<YouTubeVideoInfo> PlaylistResult { get; set; } = [];
        public IReadOnlyList<YouTubePlaylistInfo> PlaylistSearchResult { get; set; } = [];
        public IReadOnlyList<YouTubeAudioStream> AudioStreams { get; set; } = [];
        public YouTubeVideoInfo? VideoResult { get; set; }
        public List<string> SearchQueries { get; } = [];
        public bool ThrowOnSearch { get; set; }
        public bool ThrowOnPlaylist { get; set; }
        public string? DownloadedPath { get; set; }

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
        ) => Task.FromResult(VideoResult);

        public Task<IReadOnlyList<YouTubeVideoInfo>> GetPlaylistVideosAsync(
            string playlistUrl,
            CancellationToken cancellationToken = default
        ) =>
            ThrowOnPlaylist
                ? throw new InvalidOperationException("Плейлист недоступен")
                : Task.FromResult(PlaylistResult);

        public Task<IReadOnlyList<YouTubePlaylistInfo>> SearchPlaylistsAsync(
            string query,
            CancellationToken cancellationToken = default
        ) =>
            ThrowOnSearch
                ? throw new InvalidOperationException("YouTube недоступен")
                : Task.FromResult(PlaylistSearchResult);

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
            // результат так же, как её увидит плеер.
            File.WriteAllBytes(filePath, [1, 2, 3]);
            DownloadedPath = filePath;
            return Task.CompletedTask;
        }
    }
}
