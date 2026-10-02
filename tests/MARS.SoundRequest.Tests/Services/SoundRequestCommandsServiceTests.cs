using Grpc.Core;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.Interfaces;
using MARS.SoundRequest.Services.SoundCloud;
using MARS.SoundRequest.Services.Spotify;
using MARS.SoundRequest.Services.YouTube;
using MARS.SoundRequest.Tests.Grpc;
using MARS.TestKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Команды SoundRequest: разбор ссылки, постановка в очередь, позиция в очереди
/// и ожидаемое время.
///
/// Разбор ссылки проверяется через <c>AddTrackAsync</c>, а не вызовом приватных
/// помощников: правильный ответ — это ответ, который увидит зритель, а не то,
/// что вернул внутренний метод.
/// </summary>
public class SoundRequestCommandsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly TestHostApplicationLifetime _lifetime = new();
    private readonly StateManager _stateManager;
    private readonly SoundRequestUserQueue _queue;
    private readonly MainPlayer _player;
    private readonly SoundRequestCommandsService _commands;
    private readonly GrpcEventBroadcaster<SoundRequestEvent> _broadcaster = new();
    private readonly Mock<SoundRequestNotifier> _notifier;

    public SoundRequestCommandsServiceTests()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"sound-request-commands-{Guid.NewGuid():N}")
            .Options;

        _factory = new TestDbContextFactory(options);
        using (var database = _factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
        }

        _notifier = new Mock<SoundRequestNotifier>(_broadcaster);
        _stateManager = new StateManager(_factory, _lifetime, NullLogger<StateManager>.Instance);
        _queue = new SoundRequestUserQueue(_factory, _lifetime, _stateManager);
        _player = new MainPlayer(
            _stateManager,
            _notifier.Object,
            new TrackEventRelay(),
            _queue,
            _factory,
            _lifetime,
            (SpotifyPlaybackService)Stub.Resolve(typeof(SpotifyPlaybackService))!,
            new Mock<MARS.Shared.Extensions.IMarsSchemaReady<MediaDbContext>>().Object,
            Options.Create(new SoundRequestConfiguration()),
            Options.Create(new SpotifySoundRequestConfiguration()),
            NullLogger<MainPlayer>.Instance
        );

        _commands = new SoundRequestCommandsService(
            new YouTubeResolver(
                NullLogger<YouTubeResolver>.Instance,
                new Mock<IHttpClientFactory>().Object
            ),
            new SpotifyResolver(CreateSpotifyApiClient()),
            new SoundCloudResolver(NullLogger<SoundCloudResolver>.Instance),
            _queue,
            _factory,
            _player,
            _stateManager,
            _notifier.Object,
            Options.Create(new SoundRequestConfiguration())
        );
    }

    public async ValueTask InitializeAsync()
    {
        await _stateManager.InitializeAsync();
        await ((Microsoft.Extensions.Hosting.IHostedService)_player).StartAsync(
            TestContext.Current.CancellationToken
        );
    }

    public void Dispose()
    {
        _player.Dispose();
        _stateManager.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EmptyQueryIsRejected()
    {
        await InitializeAsync();

        Assert.Equal(
            "Неверные параметры запроса",
            await _commands.AddTrackAsync("  ", "viewer", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task WithoutViewerIdQueryIsRejected()
    {
        await InitializeAsync();

        Assert.Equal(
            "Неверные параметры запроса",
            await _commands.AddTrackAsync(
                "https://youtu.be/abc",
                string.Empty,
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Пока плеер остановлен, приём реквестов закрыт: иначе треки копились бы
    /// в очереди и включались разом при первом же запуске.
    /// </summary>
    [Fact]
    public async Task StoppedPlayerRefusesRequests()
    {
        await InitializeAsync();

        var answer = await _commands.AddTrackAsync(
            "https://youtu.be/abc",
            "viewer",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Прием реквестов приостановлен - плеер остановлен", answer);
    }

    /// <summary>
    /// Ссылка YouTube без сети не превращается в «неверные параметры»: команда
    /// отвечает отдельно о том, что видео не распозналось. Иначе зритель искал
    /// бы ошибку в своём запросе, хотя запрос был правильный.
    /// </summary>
    [Fact]
    public async Task LinkIsRecognisedButNotResolvedWithoutNetwork()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var answer = await _commands.AddTrackAsync(
            "https://youtu.be/dQw4w9WgXcQ",
            "viewer",
            TestContext.Current.CancellationToken
        );

        Assert.Contains("не удалось распознать", answer);
        Assert.NotEqual("Неверные параметры запроса", answer);
    }

    /// <summary>
    /// Ссылки всех трёх источников распознаются, а не-youtube строка идёт в
    /// поиск: иначе реквест отваливался бы с «не найдено» вместо поиска.
    /// </summary>
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://open.spotify.com/track/4cOdK2wGLETKBW3PvgPWqT")]
    [InlineData("https://soundcloud.com/artist/track")]
    public async Task KnownLinksAreRecognisedAsLinks(string link)
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var answer = await _commands.AddTrackAsync(
            link,
            "viewer",
            TestContext.Current.CancellationToken
        );

        Assert.NotEqual("SoundRequest отключен в конфигурации", answer);
    }

    [Fact]
    public async Task CurrentSongIsReported()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var song = await _commands.GetCurrentSongAsync(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(song));
    }

    [Fact]
    public async Task QueuePositionOfUnknownViewerIsReported()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var position = await _commands.GetUserQueuePositionAsync("никого-нет");

        Assert.False(string.IsNullOrWhiteSpace(position));
    }

    [Fact]
    public async Task ClearingEmptyQueueIsSafe()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var answer = await _commands.ClearQueueAsync(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(answer));
        Assert.Empty(await _queue.GetQueueAsync());
    }

    [Fact]
    public async Task PauseAndResumeStopAndContinuePlayback()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        await _commands.PausePlaybackAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PlaybackState.Paused, _player.GetState().State);

        await _commands.ResumePlaybackAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PlaybackState.Playing, _player.GetState().State);
    }

    [Fact]
    public async Task StopPlaybackIsAcknowledged()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var answer = await _commands.StopPlaybackAsync(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(answer));
        Assert.Equal(PlaybackState.Stopped, _player.GetState().State);
    }

    /// <summary>
    /// Отмена собственного реквеста убирает трек из очереди и отвечает зрителю:
    /// иначе человек ждал бы трек, который уже не сыграет.
    /// </summary>
    [Fact]
    public async Task CancellingOwnRequestRemovesTrack()
    {
        await InitializeAsync();
        await StartPlayingAsync();
        await _queue.AddToQueueAsync(Track("трек-2"), "viewer", DateTime.UtcNow);

        var answer = await _commands.CancelLastTrackAsync(
            "viewer",
            TestContext.Current.CancellationToken
        );

        Assert.False(string.IsNullOrWhiteSpace(answer));
        Assert.Single(await _queue.GetQueueAsync());
    }

    [Fact]
    public async Task CancellingWithoutRequestsIsReported()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var answer = await _commands.CancelLastTrackAsync(
            "viewer",
            TestContext.Current.CancellationToken
        );

        Assert.False(string.IsNullOrWhiteSpace(answer));
    }

    /// <summary>
    /// Пустой идентификатор отвергается словами, а не ошибкой: иначе зритель
    /// видел бы «Ошибка при выполнении» вместо понятного отказа.
    /// </summary>
    [Fact]
    public async Task EmptyIdIsRejectedWithReadableAnswer()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var played = await _commands.PlayQueueItemNowAsync(
            Guid.Empty,
            TestContext.Current.CancellationToken
        );
        var reordered = await _commands.ReorderQueueItemAsync(
            Guid.Empty,
            0,
            TestContext.Current.CancellationToken
        );

        Assert.Equal("❌ ID трека не может быть пустым", played);
        Assert.Equal("❌ ID элемента не может быть пустым", reordered);
    }

    /// <summary>
    /// Неизвестный идентификатор не роняет команду: ответ содержит ошибку,
    /// а не исключение, иначе кнопка в чате давала бы 500.
    /// </summary>
    [Fact]
    public async Task UnknownIdIsReportedAsError()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var played = await _commands.PlayQueueItemNowAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );
        var reordered = await _commands.ReorderQueueItemAsync(
            Guid.NewGuid(),
            0,
            TestContext.Current.CancellationToken
        );

        Assert.False(string.IsNullOrWhiteSpace(played));
        Assert.False(string.IsNullOrWhiteSpace(reordered));
    }

    /// <summary>
    /// Уведомление подписчиков уходит при смене состава очереди: без него
    /// оверлей показывал бы старый список.
    /// </summary>
    [Fact]
    public async Task QueueChangesAreBroadcast()
    {
        await InitializeAsync();
        await StartPlayingAsync();
        using var subscription = _broadcaster.Subscribe("commands-test");
        var writer = new RecordingWriter();
        using var pumpCancellation = new CancellationTokenSource();
        var pump = _broadcaster.PumpAsync(subscription, writer, pumpCancellation.Token);

        await _commands.ClearQueueAsync(TestContext.Current.CancellationToken);

        for (var attempt = 0; attempt < 100 && writer.Messages.Count == 0; attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        await pumpCancellation.CancelAsync();
        await pump;

        Assert.Contains(
            writer.Messages,
            message => message.EventCase == SoundRequestEvent.EventOneofCase.QueueChanged
        );
    }

    /// <summary>
    /// Очередь подписчика внутри broadcaster'а внутренняя, поэтому событие
    /// читается через public-метод Pump: так проверяется ровно то, что получает
    /// подписчик, а не внутренняя структура.
    /// </summary>
    private sealed class RecordingWriter : IAsyncStreamWriter<SoundRequestEvent>
    {
        public List<SoundRequestEvent> Messages { get; } = [];

        public Task WriteAsync(SoundRequestEvent message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task CompleteAsync() => Task.CompletedTask;

        public WriteOptions? WriteOptions { get; set; }
    }

    /// <summary>
    /// Spotify-цепочка собирается вручную: клиент и сервис авторизации ходят в
    /// корневую настройку своей БД, а заглушка фабрики контекстов вернула бы
    /// прокси, который Moq не умеет построить — у DbContext нет конструктора без
    /// параметров. Настоящая фабрика даёт пустую базу, и путь Spotify доходит до
    /// «нет учётных данных», что и есть проверяемое поведение.
    /// </summary>
    private SpotifyApiClient CreateSpotifyApiClient()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(new StubHandler()));

        var auth = new SpotifyAuthService(
            _factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );

        return new SpotifyApiClient(
            httpClientFactory.Object.CreateClient("spotify"),
            auth,
            Options.Create(new SpotifySoundRequestConfiguration()),
            NullLogger<SpotifyApiClient>.Instance
        );
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{}"),
                }
            );
    }

    /// <summary>
    /// Готовый трек добавляется без поиска: найденный трек не должен искаться
    /// заново, а его название попадает в ответ.
    /// </summary>
    [Fact]
    public async Task ResolvedTrackIsAddedWithoutSearch()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var answer = await _commands.AddTrackAsync(
            Track("найденный трек"),
            "viewer",
            TestContext.Current.CancellationToken
        );

        Assert.Contains("найденный трек", answer);
        Assert.Equal(2, (await _queue.GetQueueAsync()).Count);
    }

    /// <summary>
    /// Трек длиннее двенадцати минут отклоняется: очередь озвучки пережила бы его,
    /// но следующие запросы ждали бы слишком долго.
    /// </summary>
    [Fact]
    public async Task TooLongTrackIsRejected()
    {
        await InitializeAsync();
        await StartPlayingAsync();
        var longTrack = Track("долгий");
        longTrack.Duration = TimeSpan.FromMinutes(20);

        var answer = await _commands.AddTrackAsync(
            longTrack,
            "viewer",
            TestContext.Current.CancellationToken
        );

        Assert.Contains("слишком длинный", answer);
        Assert.Single(await _queue.GetQueueAsync());
    }

    /// <summary>
    /// Без идентификатора зрителя трек не добавляется: иначе в очереди появился бы
    /// трек, который никто не отменяет.
    /// </summary>
    [Fact]
    public async Task TrackWithoutViewerIsRejected()
    {
        await InitializeAsync();

        var answer = await _commands.AddTrackAsync(
            Track("трек"),
            "  ",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Неверные параметры запроса", answer);
    }

    /// <summary>
    /// Плейлист при остановленном плеере не разбирается: приём реквестов
    /// приостановлен, и обращение к сети было бы напрасным.
    /// </summary>
    [Fact]
    public async Task PlaylistIsRejectedWhilePlayerIsStopped()
    {
        await InitializeAsync();

        var answer = await _commands.AddPlaylistAsync(
            "https://youtube.com/playlist?list=PL1",
            "viewer",
            10,
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Прием реквестов приостановлен - плеер остановлен", answer);
    }

    /// <summary>
    /// Плейлист без треков честно сообщает об этом, а не добавляет в очередь
    /// случайные записи из выдачи.
    /// </summary>
    [Fact]
    public async Task PlaylistWithoutTracksIsReported()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var answer = await _commands.AddPlaylistAsync(
            "https://example.org/not-a-playlist",
            "viewer",
            10,
            TestContext.Current.CancellationToken
        );

        Assert.False(string.IsNullOrWhiteSpace(answer));
        Assert.Single(await _queue.GetQueueAsync());
    }

    /// <summary>
    /// Время ожидания считается по остатку текущего трека и длине следующих: иначе
    /// зритель получил бы неверный номер в очереди.
    /// </summary>
    [Fact]
    public async Task WaitTimeIsCalculatedFromQueue()
    {
        await InitializeAsync();
        await StartPlayingAsync();

        var waitTime = await InvokeAsync(
            "CalculateWaitTimeAsync",
            [2],
            TestContext.Current.CancellationToken
        );

        Assert.True(waitTime >= TimeSpan.Zero);
    }

    /// <summary>
    /// Ожидание форматируется по-русски и с «~»: точный прогноз всё равно врёт из-за
    /// рекламы между треками.
    /// </summary>
    [Theory]
    [InlineData(30, "30 сек")]
    [InlineData(60, "минута")]
    [InlineData(95, "1 мин 35 сек")]
    [InlineData(600, "10 мин")]
    [InlineData(3900, "1 ч 5 мин")]
    public void WaitTimeIsFormattedForViewer(int seconds, string expected)
    {
        var formatted = Invoke<string>("FormatWaitTime", [TimeSpan.FromSeconds(seconds)]);

        Assert.Contains(expected, formatted);
    }

    /// <summary>
    /// Ссылка на YouTube разбирается в идентификатор: без него не найдётся трек по
    /// ссылке.
    /// </summary>
    [Fact]
    public void YouTubeVideoIdIsExtracted()
    {
        Assert.Equal(
            "abc123",
            Invoke<string?>("ExtractYouTubeVideoId", ["https://youtu.be/abc123"])
        );
        Assert.Null(Invoke<string?>("ExtractYouTubeVideoId", ["https://example.org/abc123"]));
    }

    /// <summary>
    /// Ожидание меньше секунды честно об этом говорит: обещать точное время после
    /// «0 сек» бессмысленно, треки начинаются не мгновенно.
    /// </summary>
    [Fact]
    public void SubSecondWaitTimeIsReported()
    {
        var formatted = Invoke<string>("FormatWaitTime", [TimeSpan.FromMilliseconds(400)]);

        Assert.Contains("меньше секунды", formatted);
    }

    private async Task<TimeSpan> InvokeAsync(
        string name,
        object?[] arguments,
        CancellationToken cancellationToken
    )
    {
        var method = typeof(SoundRequestCommandsService).GetMethod(
            name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;
        var lastArgument = method.GetParameters().Length - 1;

        if (method.GetParameters()[lastArgument].ParameterType == typeof(CancellationToken))
        {
            arguments = [.. arguments, cancellationToken];
        }

        return await (Task<TimeSpan>)method.Invoke(_commands, arguments)!;
    }

    private T Invoke<T>(string name, object?[] arguments)
    {
        var method = typeof(SoundRequestCommandsService).GetMethod(
            name,
            System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.NonPublic
        )!;

        return (T)method.Invoke(method.IsStatic ? null : _commands, arguments)!;
    }

    private static BaseTrackInfo Track(string name) =>
        new()
        {
            TrackName = name,
            Url = new Uri($"https://mars.example.org/{name}"),
            VideoId = Guid.NewGuid().ToString("N"),
            Duration = TimeSpan.FromMinutes(3),
        };

    private async Task StartPlayingAsync()
    {
        var queueItem = await _queue.AddToQueueAsync(Track("первый"), "viewer", DateTime.UtcNow);
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);
    }
}
