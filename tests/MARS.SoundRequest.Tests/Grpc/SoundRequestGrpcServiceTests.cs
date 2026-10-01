using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Grpc;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoundRequestServiceClient = MARS.Shared.Grpc.SoundRequest.SoundRequestService.SoundRequestServiceClient;

namespace MARS.SoundRequest.Tests.Grpc;

public class SoundRequestGrpcServiceTests : IAsyncLifetime
{
    private static readonly TimeSpan StreamReadTimeout = TimeSpan.FromSeconds(10);

    private readonly TestDbContextFactory _factory;
    private readonly FakePlayerController _player = new();
    private readonly TrackEventRelay _relay = new();
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private SoundRequestServiceClient _client = null!;
    private StateManager _stateManager = null!;

    public SoundRequestGrpcServiceTests()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"sound-request-{Guid.NewGuid():N}")
            .Options;

        _factory = new TestDbContextFactory(options);

        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public async ValueTask InitializeAsync()
    {
        _stateManager = new StateManager(
            _factory,
            new TestHostApplicationLifetime(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<StateManager>.Instance
        );
        await _stateManager.InitializeAsync();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<IDbContextFactory<MediaDbContext>>(_factory);
        builder.Services.AddSingleton(_stateManager);
        builder.Services.AddSingleton<IPlayerController>(_player);
        builder.Services.AddSingleton(_relay);
        builder.Services.AddSingleton(new GrpcEventBroadcaster<SoundRequestEvent>());

        _app = builder.Build();
        _app.MapGrpcService<SoundRequestGrpcService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _channel = GrpcChannel.ForAddress(
            "http://sound-request.test",
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() }
        );
        _client = new SoundRequestServiceClient(_channel);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_SendsCurrentPlayerStateFirst()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));

        Assert.Equal(
            SoundRequestEvent.EventOneofCase.PlayerStateChange,
            call.ResponseStream.Current.EventCase
        );
        Assert.Equal(100f, call.ResponseStream.Current.PlayerStateChange.Volume, precision: 3);
    }

    [Fact]
    public async Task FrontStateChange_UpdatesStateAndLoadsQueueItemOnPlaying()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(await MoveNextAsync(call));

        await _client.FrontStateChangeAsync(
            new FrontStateChangeRequest
            {
                State = new PlayerStateSnapshot
                {
                    State = SoundRequestPlaybackState.Playing,
                    Volume = 42,
                    IsMuted = true,
                    VideoState = SoundRequestVideoDisplay.NoVideo,
                    HasCurrentTrackProgress = true,
                    CurrentTrackProgressSeconds = 17,
                },
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(1, _player.EnsureCurrentQueueItemLoadedCalls);

        var state = await _stateManager.GetStateAsync();

        Assert.Equal(PlaybackState.Playing, state.State);
        Assert.Equal(42f, state.Volume);
        Assert.True(state.IsMuted);
        Assert.Equal(VideoDisplay.NoVideo, state.VideoState);
        Assert.Equal(TimeSpan.FromSeconds(17), state.CurrentTrackProgress);
    }

    [Fact]
    public async Task FrontStateChange_DoesNotLoadQueueItemWhenPaused()
    {
        await _client.FrontStateChangeAsync(
            new FrontStateChangeRequest
            {
                State = new PlayerStateSnapshot { State = SoundRequestPlaybackState.Paused },
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(0, _player.EnsureCurrentQueueItemLoadedCalls);
    }

    [Fact]
    public async Task TrackProgress_StoresProgressWithoutBroadcast()
    {
        await _client.TrackProgressAsync(
            new TrackProgressRequest { Seconds = 33 },
            cancellationToken: TestContext.Current.CancellationToken
        );

        var state = await _stateManager.GetStateAsync();

        Assert.Null(state.CurrentTrackProgress);
    }

    [Fact]
    public async Task TrackEvents_AreRelayedToSubscribers()
    {
        BaseTrackInfo? started = null;
        BaseTrackInfo? ended = null;
        BaseTrackInfo? failed = null;
        _relay.OnStarted += track =>
        {
            started = track;
            return Task.CompletedTask;
        };
        _relay.OnEnded += track =>
        {
            ended = track;
            return Task.CompletedTask;
        };
        _relay.OnError += track =>
        {
            failed = track;
            return Task.CompletedTask;
        };

        var track = new TrackInfo
        {
            Id = "11111111-2222-3333-4444-555555555555",
            TrackName = "Хижина в лесу",
            DurationSeconds = 245,
            Url = "https://youtu.be/abc",
        };
        track.Authors.Add("Pyro");

        await _client.StartedAsync(
            new TrackEventRequest { Track = track },
            cancellationToken: TestContext.Current.CancellationToken
        );
        await _client.EndedAsync(
            new TrackEventRequest { Track = track },
            cancellationToken: TestContext.Current.CancellationToken
        );
        await _client.ErrorPlayingAsync(
            new TrackEventRequest { Track = track },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("Хижина в лесу", started?.TrackName);
        Assert.Equal(TimeSpan.FromSeconds(245), ended?.Duration);
        Assert.Equal(["Pyro"], failed?.Authors ?? []);
    }

    [Fact]
    public async Task SkipTrack_AndPlayPrevious_ReachPlayerController()
    {
        await _client.SkipTrackAsync(
            new SkipTrackRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await _client.PlayPreviousAsync(
            new PlayPreviousRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(1, _player.SkipCalls);
        Assert.Equal(1, _player.PlayPreviousCalls);
    }

    private static async Task<bool> MoveNextAsync(AsyncServerStreamingCall<SoundRequestEvent> call)
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        return await call.ResponseStream.MoveNext(timeout.Token);
    }
}
