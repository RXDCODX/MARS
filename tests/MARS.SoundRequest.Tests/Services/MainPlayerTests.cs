using MARS.Shared.Extensions;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.Spotify;
using MARS.SoundRequest.Tests.Grpc;
using MARS.TestKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Плеер поверх настоящего состояния и настоящей БД в памяти.
///
/// Плеер — самый крупный класс сервиса, и почти всё в нём — переходы состояния:
/// пауза, снятие с паузы, пропуск, конец трека. Проверять их с заглушками
/// бессмысленно: заглушка StateManager ничего не хранит, и тест утверждал бы,
/// что вызов состояние изменил. Состояние и очередь здесь настоящие, Spotify
/// отключён настройками — с ним не ходим в сеть.
/// </summary>
public class MainPlayerTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly TestHostApplicationLifetime _lifetime = new();
    private readonly StateManager _stateManager;
    private readonly SoundRequestUserQueue _queue;
    private readonly MainPlayer _player;
    private bool _started;

    public MainPlayerTests()
    {
        _factory = new TestDbContextFactory();
        using (var database = _factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
        }

        _stateManager = new StateManager(_factory, _lifetime, NullLogger<StateManager>.Instance);
        _queue = new SoundRequestUserQueue(_factory, _lifetime, _stateManager);

        _player = new MainPlayer(
            _stateManager,
            new Mock<SoundRequestNotifier>(new GrpcEventBroadcaster<SoundRequestEvent>()).Object,
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
    public async Task StartedPlayerHasDefaultState()
    {
        await StartAsync();

        var state = _player.GetState();

        Assert.Equal(PlaybackState.Stopped, state.State);
        Assert.False(state.IsMuted);
        Assert.Equal(100f, state.Volume);
        Assert.Null(state.CurrentQueueItem);
    }

    [Fact]
    public async Task PlayMovesTrackToCurrentAndPlayingState()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");

        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);

        var state = _player.GetState();
        Assert.Equal(PlaybackState.Playing, state.State);
        Assert.Equal("трек-1", state.CurrentQueueItem!.Track!.TrackName);
    }

    [Fact]
    public async Task PauseAndResumeToggleState()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);

        await _player.PauseAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PlaybackState.Paused, _player.GetState().State);

        await _player.ResumeAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PlaybackState.Playing, _player.GetState().State);
    }

    [Fact]
    public async Task StopClearsPlaybackState()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);

        await _player.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PlaybackState.Stopped, _player.GetState().State);
    }

    /// <summary>
    /// Mute не должен терять громкость: после разглушения трек звучит на прежнем
    /// уровне, а не на нуле.
    /// </summary>
    [Fact]
    public async Task MuteKeepsVolumeForUnmute()
    {
        await StartAsync();
        await _player.SetVolumeAsync(40f, TestContext.Current.CancellationToken);

        await _player.MuteAsync(TestContext.Current.CancellationToken);
        Assert.True(_player.GetState().IsMuted);

        await _player.UnmuteAsync(TestContext.Current.CancellationToken);
        var state = _player.GetState();
        Assert.False(state.IsMuted);
        Assert.Equal(40f, state.Volume);
    }

    [Fact]
    public async Task VolumeIsStoredAsGiven()
    {
        await StartAsync();

        await _player.SetVolumeAsync(77f, TestContext.Current.CancellationToken);

        Assert.Equal(77f, _player.GetState().Volume);
    }

    /// <summary>
    /// Пропуск на пустой очереди переводит плеер в ожидание: иначе на стриме
    /// остался бы трек, которого уже нет.
    /// </summary>
    [Fact]
    public async Task SkipOnEmptyQueueClearsCurrentItem()
    {
        await StartAsync();

        await _player.SkipAsync(TestContext.Current.CancellationToken);

        Assert.Null(_player.GetState().CurrentQueueItem);
    }

    /// <summary>
    /// «Играть» запускает текущий трек, а не продвигает очередь.
    ///
    /// Иначе только что добавленный трек (он единственный, с порядком 0)
    /// сдвигался в историю и не воспроизводился вовсе: на стриме нажатие на
    /// play не давало звука, а трек молча уходил в проигранные.
    /// </summary>
    [Fact]
    public async Task PlayingStartsThePendingTrackInsteadOfSkippingIt()
    {
        await StartAsync();
        await AddToQueueAsync("трек-1");

        await _player.PlayAsync();

        var state = _player.GetState();
        Assert.Equal(PlaybackState.Playing, state.State);
        Assert.Equal("трек-1", state.CurrentQueueItem?.Track?.TrackName);
    }

    /// <summary>
    /// Уже загруженный текущий трек «играть» запускает, а не перематывает
    /// очередь дальше: иначе play на паузе проглатывал бы трек.
    /// </summary>
    [Fact]
    public async Task PlayingResumesFromTheCurrentTrack()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await AddToQueueAsync("трек-2");
        await _player.EnsureCurrentQueueItemLoadedAsync();

        await _player.PlayAsync();

        Assert.Equal("трек-1", _player.GetState().CurrentQueueItem?.Track?.TrackName);
        Assert.Equal(queueItem.Id, _player.GetState().CurrentQueueItem?.Id);
    }

    /// <summary>
    /// На паузе «играть» снимает паузу, а не начинает следующий трек.
    /// </summary>
    [Fact]
    public async Task PlayingWhilePausedResumesSameTrack()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);
        await _player.PauseAsync(TestContext.Current.CancellationToken);

        await _player.PlayAsync();

        Assert.Equal(PlaybackState.Playing, _player.GetState().State);
        Assert.Equal(queueItem.Id, _player.GetState().CurrentQueueItem?.Id);
    }

    [Fact]
    public async Task TogglePlayPauseFollowsState()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);

        await _player.TogglePlayPauseAsync();
        Assert.Equal(PlaybackState.Paused, _player.GetState().State);

        await _player.TogglePlayPauseAsync();
        Assert.Equal(PlaybackState.Playing, _player.GetState().State);
    }

    /// <summary>
    /// Конец последнего трека в очереди очищает текущий: иначе плеер навсегда
    /// остался бы на уже закончившемся треке.
    /// </summary>
    [Fact]
    public async Task TrackEndedOnLastItemClearsCurrent()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);

        await _player.OnTrackEndedAsync(queueItem.Track!);

        Assert.Null(_player.GetState().CurrentQueueItem);
    }

    [Fact]
    public async Task TrackEndedOfForeignTrackChangesNothing()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);

        await _player.OnTrackEndedAsync(Track("чужой"));

        Assert.Equal(queueItem.Id, _player.GetState().CurrentQueueItem?.Id);
    }

    [Fact]
    public async Task TrackStartedOnlyNotifiesSubscribers()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");

        await _player.OnTrackStartedAsync(queueItem.Track!);

        Assert.Equal(PlaybackState.Stopped, _player.GetState().State);
    }

    [Fact]
    public async Task TrackErrorOnLastTrackClearsCurrentItem()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);

        await _player.OnTrackErrorAsync(queueItem.Track!);

        Assert.Null(_player.GetState().CurrentQueueItem);
    }

    [Fact]
    public async Task QueueIsReturnedAndItemsAreRemovable()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");

        Assert.Single(await _player.GetQueueAsync());

        await _player.RemoveQueueItemAsync(queueItem.Id);

        Assert.Empty(await _player.GetQueueAsync());
    }

    [Fact]
    public async Task HistoryIsFilledAfterSkip()
    {
        await StartAsync();
        Assert.Empty(await _player.GetHistoryAsync());
        Assert.Empty(await _player.GetHistoryQueueItemsAsync());

        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);
        await _player.SkipAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(await _player.GetHistoryAsync());
    }

    [Fact]
    public async Task PreviousFromHistoryIsPlayedBack()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");
        await _player.PlayAsync(queueItem, TestContext.Current.CancellationToken);
        await _player.SkipAsync(TestContext.Current.CancellationToken);

        await _player.PlayPreviousFromHistoryAsync();

        Assert.Equal(PlaybackState.Playing, _player.GetState().State);
    }

    [Fact]
    public async Task PreviousFromEmptyHistoryKeepsState()
    {
        await StartAsync();

        await _player.PlayPreviousFromHistoryAsync();

        Assert.Null(_player.GetState().CurrentQueueItem);
    }

    [Fact]
    public async Task PlayingByIdTakesItemFromQueue()
    {
        await StartAsync();
        await AddToQueueAsync("трек-1");
        var second = await AddToQueueAsync("трек-2");

        await _player.PlayQueueItemAsync(second.Id);

        Assert.Equal("трек-2", _player.GetState().CurrentQueueItem?.Track?.TrackName);
    }

    [Fact]
    public async Task PlayingByUnknownIdChangesNothing()
    {
        await StartAsync();

        await _player.PlayQueueItemAsync(Guid.NewGuid());

        Assert.Null(_player.GetState().CurrentQueueItem);
    }

    [Fact]
    public async Task EnsureCurrentQueueItemIsLoadedFromQueue()
    {
        await StartAsync();
        var queueItem = await AddToQueueAsync("трек-1");

        await _player.EnsureCurrentQueueItemLoadedAsync();

        Assert.Equal(queueItem.Id, _player.GetState().CurrentQueueItem?.Id);
    }

    [Fact]
    public async Task VideoDisplayIsStoredInState()
    {
        await StartAsync();

        await _player.SetVideoDisplayAsync(
            VideoDisplay.AudioOnly,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(VideoDisplay.AudioOnly, _player.GetState().VideoState);
    }

    /// <summary>
    /// Url и VideoId уникальны на каждый трек: очередь ищет существующий трек по
    /// VideoId или Url и обновляет его, а не добавляет новый — с одинаковыми
    /// ссылками два трека слились бы в один.
    /// </summary>
    private static BaseTrackInfo Track(string name) =>
        new()
        {
            TrackName = name,
            Url = new Uri($"https://mars.example.org/{name}"),
            VideoId = Guid.NewGuid().ToString("N"),
        };

    private async Task<QueueItem> AddToQueueAsync(string name) =>
        await _queue.AddToQueueAsync(Track(name), "viewer", DateTime.UtcNow);

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
}
