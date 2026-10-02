using System.Reflection;
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Внутренние решения плеера: разбор источника, фильтр площадок и ретрансляция
/// событий трека.
///
/// События трека приходят от плеера и разворачиваются в состояние и историю, и если
/// ретрансляция молча перестанет работать, трек не будет переходить дальше по
/// очереди. Проверяется именно это.
/// </summary>
public class MainPlayerInternalsTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly TestHostApplicationLifetime _lifetime = new();
    private readonly StateManager _stateManager;
    private readonly MainPlayer _player;

    public MainPlayerInternalsTests()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"main-player-internals-{Guid.NewGuid():N}")
            .Options;

        _factory = new TestDbContextFactory(options);
        using (var database = _factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
        }

        _stateManager = new StateManager(_factory, _lifetime, NullLogger<StateManager>.Instance);

        _player = new MainPlayer(
            _stateManager,
            new Mock<SoundRequestNotifier>(new GrpcEventBroadcaster<SoundRequestEvent>()).Object,
            new TrackEventRelay(),
            new SoundRequestUserQueue(_factory, _lifetime, _stateManager),
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
        _player.Dispose();
        _stateManager.Dispose();
    }

    /// <summary>
    /// Источник разбирается по имени и по номеру: настройки и база хранят разные
    /// представления одного и того же.
    /// </summary>
    [Fact]
    public void ProviderIsParsedByName()
    {
        var parsed = TryParse("spotify", out var provider);

        Assert.True(parsed);
        Assert.Equal(SoundRequestProvider.Spotify, provider);
    }

    [Fact]
    public void ProviderIsParsedByNumber()
    {
        var parsed = TryParse(((int)SoundRequestProvider.Spotify).ToString(), out var provider);

        Assert.True(parsed);
        Assert.Equal(SoundRequestProvider.Spotify, provider);
    }

    /// <summary>
    /// Неизвестный источник трактуется как YouTube: очередь должна работать даже с
    /// испорченной настройкой.
    /// </summary>
    [Theory]
    [InlineData("что-то новое")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownProviderFallsBackToYouTube(string? raw)
    {
        var parsed = TryParse(raw!, out var provider);

        Assert.False(parsed);
        Assert.Equal(SoundRequestProvider.YouTube, provider);
    }

    /// <summary>
    /// По умолчанию разрешены все площадки: иначе после смены формата настройки
    /// сервис перестал бы принимать заявки.
    /// </summary>
    [Fact]
    public void DefaultConfigurationAllowsEveryPlatform()
    {
        Assert.True(InvokeIsPlatformAllowed("YouTube"));
        Assert.True(InvokeIsPlatformAllowed("Spotify"));
    }

    /// <summary>
    /// Пустой список запрещает всё: явное «ничего не разрешено» должно означать
    /// запрет, а не «как обычно».
    /// </summary>
    [Fact]
    public void EmptyPlatformListForbidsEverything()
    {
        var player = CreatePlayer(new SoundRequestConfiguration { EnabledPlatforms = [] });

        Assert.False(InvokeIsPlatformAllowed(player, "Spotify"));
    }

    [Fact]
    public void ListedPlatformIsAllowed()
    {
        var player = CreatePlayer(new SoundRequestConfiguration { EnabledPlatforms = ["Spotify"] });

        Assert.True(InvokeIsPlatformAllowed(player, "Spotify"));
        Assert.False(InvokeIsPlatformAllowed(player, "YouTube"));
    }

    /// <summary>
    /// Регистр площадки не важен: в настройках его пишут по-разному.
    /// </summary>
    [Fact]
    public void PlatformMatchIgnoresCase()
    {
        var player = CreatePlayer(new SoundRequestConfiguration { EnabledPlatforms = ["spotify"] });

        Assert.True(InvokeIsPlatformAllowed(player, "Spotify"));
    }

    /// <summary>
    /// Событие старта пересобирает состояние для подписчиков: без него фронтенд не
    /// узнал бы, что трек пошёл.
    /// </summary>
    [Fact]
    public async Task StartedEventReachesThePlayer()
    {
        var queue = new SoundRequestUserQueue(_factory, _lifetime, _stateManager);
        var item = await queue.AddToQueueAsync(Track("трек"), "viewer", DateTime.UtcNow);
        await _player.PlayAsync(item, TestContext.Current.CancellationToken);

        // Релей обязан дойти до плеера: иначе событие начала трека терялось бы, и
        // очередь не сдвинулась бы после окончания.
        await Relay("TrackEventRelayOnStarted", Track("трек"));

        Assert.Equal(PlaybackState.Playing, _player.GetState().State);
    }

    [Fact]
    public async Task ErrorEventStopsPlayback()
    {
        var queue = new SoundRequestUserQueue(_factory, _lifetime, _stateManager);
        var first = await queue.AddToQueueAsync(Track("трек"), "viewer", DateTime.UtcNow);
        await _player.PlayAsync(first, TestContext.Current.CancellationToken);

        await Relay("TrackEventRelayOnError", Track("трек"));

        Assert.NotEqual(PlaybackState.Playing, _player.GetState().State);
    }

    private static BaseTrackInfo Track(string name) =>
        new()
        {
            TrackName = name,
            Url = new Uri($"https://mars.example.org/{name}"),
            VideoId = Guid.NewGuid().ToString("N"),
            Duration = TimeSpan.FromMinutes(3),
        };

    /// <summary>
    /// Релей вызывается так же, как его зовёт плеер: имя метода — единственный
    /// контракт между ними.
    /// </summary>
    private Task Relay(string name, BaseTrackInfo track)
    {
        var method = typeof(MainPlayer).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(_player, [track])!;
    }

    private static Task RelayAsync(MainPlayer player, string name, BaseTrackInfo track)
    {
        var method = typeof(MainPlayer).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(player, [track])!;
    }

    private static bool TryParse(string raw, out SoundRequestProvider provider)
    {
        var method = typeof(MainPlayer).GetMethod(
            "TryParseProvider",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;
        object?[] arguments = [raw, null];

        var parsed = (bool)method.Invoke(null, arguments)!;
        provider = (SoundRequestProvider)arguments[1]!;

        return parsed;
    }

    private bool InvokeIsPlatformAllowed(string platform) =>
        InvokeIsPlatformAllowed(_player, platform);

    private static bool InvokeIsPlatformAllowed(MainPlayer player, string platform)
    {
        var method = typeof(MainPlayer).GetMethod(
            "IsPlatformAllowed",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (bool)method.Invoke(player, [platform])!;
    }

    private MainPlayer CreatePlayer(SoundRequestConfiguration configuration) =>
        new(
            _stateManager,
            new Mock<SoundRequestNotifier>(new GrpcEventBroadcaster<SoundRequestEvent>()).Object,
            new TrackEventRelay(),
            new SoundRequestUserQueue(_factory, _lifetime, _stateManager),
            _factory,
            _lifetime,
            (SpotifyPlaybackService)Stub.Resolve(typeof(SpotifyPlaybackService))!,
            new Mock<IMarsSchemaReady<MediaDbContext>>().Object,
            Options.Create(configuration),
            Options.Create(new SpotifySoundRequestConfiguration()),
            NullLogger<MainPlayer>.Instance
        );
}
