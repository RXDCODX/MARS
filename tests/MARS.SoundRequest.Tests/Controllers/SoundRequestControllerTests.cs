using System.Net;
using MARS.Shared.Extensions;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.Shared.Models;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Controllers;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.SoundCloud;
using MARS.SoundRequest.Services.Spotify;
using MARS.SoundRequest.Services.YouTube;
using MARS.SoundRequest.Tests.Grpc;
using MARS.TestKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Controllers;

/// <summary>
/// Контроллер запросов звука поверх настоящей очереди в памяти.
///
/// Проверяется не только «метод вернул результат» (это делает обход
/// <c>ControllerActionTests</c>), а смысл ответов: пустой id, отсутствующий
/// элемент и пустой запрос обязаны давать внятную ошибку, а не 200 с «готово».
/// </summary>
public class SoundRequestControllerTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly TestHostApplicationLifetime _lifetime = new();
    private readonly StateManager _stateManager;
    private readonly SoundRequestUserQueue _queue;
    private readonly MainPlayer _player;
    private readonly SoundRequestController _controller;
    private bool _started;

    public SoundRequestControllerTests()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"sound-request-controller-{Guid.NewGuid():N}")
            .Options;

        _factory = new TestDbContextFactory(options);
        using (var database = _factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
        }

        var notifier = new Mock<SoundRequestNotifier>(
            new GrpcEventBroadcaster<SoundRequestEvent>()
        );
        _stateManager = new StateManager(_factory, _lifetime, NullLogger<StateManager>.Instance);
        _queue = new SoundRequestUserQueue(_factory, _lifetime, _stateManager);
        _player = new MainPlayer(
            _stateManager,
            notifier.Object,
            new TrackEventRelay(),
            _queue,
            _factory,
            _lifetime,
            (SpotifyPlaybackService)Stub.Resolve(typeof(SpotifyPlaybackService))!,
            new Mock<IMarsSchemaReady<MediaDbContext>>().Object,
            Options.Create(new SoundRequestConfiguration()),
            Options.Create(new SpotifySoundRequestConfiguration()),
            NullLogger<MainPlayer>.Instance
        );

        var service = new SoundRequestCommandsService(
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
            notifier.Object,
            Options.Create(new SoundRequestConfiguration())
        );

        _controller = new SoundRequestController(
            _player,
            service,
            _queue,
            NullLogger<SoundRequestController>.Instance
        );
    }

    public void Dispose()
    {
        if (_started)
        {
            ((IHostedService)_player).StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        _player.Dispose();
        _stateManager.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task StateIsReturnedAsSuccessfulEnvelope()
    {
        await StartAsync();

        var result = Unwrap(_controller.GetPlayerState());

        Assert.True(result.Success);
        Assert.Equal(PlaybackState.Stopped, result.Result!.State);
    }

    [Fact]
    public async Task QueueIsReturnedAsSuccessfulEnvelope()
    {
        await StartAsync();
        await AddToQueueAsync("трек-1");

        var result = Unwrap(await _controller.GetQueue(TestContext.Current.CancellationToken));

        Assert.True(result.Success);
        Assert.Single(result.Result!);
    }

    [Fact]
    public async Task HistoryIsReturnedForRequestedCount()
    {
        await StartAsync();

        var result = Unwrap(await _controller.GetHistory(5, TestContext.Current.CancellationToken));

        Assert.True(result.Success);
        Assert.Empty(result.Result!);
    }

    [Fact]
    public async Task HistoryQueueItemsAreReturnedAsEnvelope()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.GetHistoryQueueItems(5, TestContext.Current.CancellationToken)
        );

        Assert.True(result.Success);
        Assert.Empty(result.Result!);
    }

    [Fact]
    public async Task CurrentSongIsReturnedAsEnvelope()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.GetCurrentSong(TestContext.Current.CancellationToken)
        );

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyQueryIsRejectedWithReason(string query)
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.AddTrack(query, TestContext.Current.CancellationToken)
        );

        Assert.False(result.Success);
        Assert.Contains("URL или название", result.ErrorMessage);
    }

    /// <summary>
    /// Префикс команды из чата срезается: без этого в очередь ушёл бы трек с
    /// именем «!sr», а зритель получил бы ошибку про несуществующую ссылку.
    /// </summary>
    [Fact]
    public async Task CommandPrefixIsTrimmedBeforeParsing()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.AddTrack("!sr", TestContext.Current.CancellationToken)
        );

        Assert.DoesNotContain("!sr", result.Result);
        Assert.Contains("Неверные параметры запроса", result.Result);
    }

    [Fact]
    public async Task UnknownQueueItemIsReportedAsMissing()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.DeleteFromQueue(Guid.NewGuid(), TestContext.Current.CancellationToken)
        );

        Assert.False(result.Success);
        Assert.Contains("не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task EmptyQueueItemIdIsRejected()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.DeleteFromQueue(Guid.Empty, TestContext.Current.CancellationToken)
        );

        Assert.False(result.Success);
        Assert.Contains("Некорректный идентификатор", result.ErrorMessage);
    }

    [Fact]
    public async Task QueuedItemIsRemoved()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");

        var result = Unwrap(
            await _controller.DeleteFromQueue(queueItem.Id, TestContext.Current.CancellationToken)
        );

        Assert.True(result.Success);
        Assert.Empty(await _queue.GetQueueAsync());
    }

    [Fact]
    public async Task PlayingQueueItemNowRejectsEmptyId()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.PlayQueueItemNow(Guid.Empty, TestContext.Current.CancellationToken)
        );

        Assert.False(result.Success);
        Assert.Contains("Некорректный идентификатор", result.ErrorMessage);
    }

    [Fact]
    public async Task PlayingQueueItemNowReportsUnknownItem()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.PlayQueueItemNow(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken
            )
        );

        Assert.Contains("не найден", result.Result);
    }

    [Fact]
    public async Task ReorderWithoutRequestIsRejected()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.ReorderQueueItem(null, TestContext.Current.CancellationToken)
        );

        Assert.False(result.Success);
        Assert.Equal("Некорректный запрос", result.ErrorMessage);
    }

    [Fact]
    public async Task ReorderWithEmptyIdIsRejected()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.ReorderQueueItem(
                new SoundRequestController.QueueReorderRequest
                {
                    QueueItemId = Guid.Empty,
                    NewPosition = 1,
                },
                TestContext.Current.CancellationToken
            )
        );

        Assert.False(result.Success);
        Assert.Equal("Некорректный запрос", result.ErrorMessage);
    }

    [Fact]
    public async Task ReorderOfUnknownItemFailsWithMessage()
    {
        await StartAsync();

        var result = Unwrap(
            await _controller.ReorderQueueItem(
                new SoundRequestController.QueueReorderRequest
                {
                    QueueItemId = Guid.NewGuid(),
                    NewPosition = 0,
                },
                TestContext.Current.CancellationToken
            )
        );

        Assert.Contains("не найден", result.Result);
    }

    [Fact]
    public async Task ReorderMovesQueuedItem()
    {
        await StartAsync();
        var first = await AddToQueueAsync("трек-1");
        await AddToQueueAsync("трек-2");

        var result = Unwrap(
            await _controller.ReorderQueueItem(
                new SoundRequestController.QueueReorderRequest
                {
                    QueueItemId = first.Id,
                    NewPosition = 1,
                },
                TestContext.Current.CancellationToken
            )
        );

        Assert.Contains("Позиция обновлена", result.Result);
        var queue = await _queue.GetQueueAsync();
        Assert.Equal(1, queue.ToList().FindIndex(item => item.Id == first.Id));
    }

    private static SpotifyApiClient CreateSpotifyApiClient()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(new StubHandler()));

        var auth = new SpotifyAuthService(
            null!,
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

    private static BaseTrackInfo Track(string name) =>
        new()
        {
            TrackName = name,
            Url = new Uri($"https://mars.example.org/{name}"),
            VideoId = Guid.NewGuid().ToString("N"),
        };

    private static OperationResult<T> Unwrap<T>(ActionResult<OperationResult<T>> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<OperationResult<T>>(ok.Value);
    }

    private static OperationResult Unwrap(ActionResult<OperationResult> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<OperationResult>(ok.Value);
    }

    private Task<QueueItem> AddToQueueAsync(string name) =>
        _queue.AddToQueueAsync(Track(name), "viewer", DateTime.UtcNow);

    private async Task StartAsync()
    {
        if (_started)
        {
            return;
        }

        await _stateManager.InitializeAsync();
        await ((IHostedService)_player).StartAsync(TestContext.Current.CancellationToken);
        _started = true;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{}"),
                }
            );
    }
}
