using Grpc.Core;
using Grpc.Net.Client;
using MARS.Scoreboard.Data;
using MARS.Scoreboard.Grpc;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Scoreboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScoreboardService = MARS.Scoreboard.Services.ScoreboardService;
using ScoreboardServiceClient = MARS.Shared.Grpc.Scoreboard.ScoreboardService.ScoreboardServiceClient;

namespace MARS.Scoreboard.Tests.Grpc;

public class ScoreboardGrpcServiceTests : IAsyncLifetime
{
    private static readonly TimeSpan StreamReadTimeout = TimeSpan.FromSeconds(10);

    private readonly TestDbContextFactory _factory;
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private ScoreboardServiceClient _client = null!;

    public ScoreboardGrpcServiceTests()
    {
        _factory = new TestDbContextFactory();
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<IDbContextFactory<ScoreboardDbContext>>(_factory);
        builder.Services.AddScoped<ScoreboardService>();
        builder.Services.AddSingleton<GrpcEventBroadcaster<ScoreboardEvent>>();

        _app = builder.Build();
        _app.MapGrpcService<ScoreboardGrpcService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _channel = GrpcChannel.ForAddress(
            "http://scoreboard.test",
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() }
        );
        _client = new ScoreboardServiceClient(_channel);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_SendsCurrentStateFirst()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var first = call.ResponseStream.Current;

        Assert.Equal(ScoreboardEvent.EventOneofCase.StateUpdated, first.EventCase);
        Assert.Equal("Tournament", first.StateUpdated.Meta.Title);
    }

    [Fact]
    public async Task UpdateState_PersistsAndBroadcastsToSubscribers()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(await MoveNextAsync(call));

        await _client.UpdateStateAsync(
            new UpdateStateRequest
            {
                State = new ScoreboardSnapshot
                {
                    Meta = new ScoreboardMeta { Title = "Bo5", FightRule = "Best of 5" },
                    AnimationDuration = 1200,
                    Player1 = new ScoreboardPlayer { Name = "Neo", Score = 2 },
                },
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var broadcast = call.ResponseStream.Current;

        var stored = await new ScoreboardService(
            _factory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ScoreboardService>.Instance
        ).GetCurrentStateAsync();

        Assert.Equal(ScoreboardEvent.EventOneofCase.StateUpdated, broadcast.EventCase);
        Assert.Equal("Bo5", broadcast.StateUpdated.Meta.Title);
        Assert.Equal(1200, broadcast.StateUpdated.AnimationDuration);
        Assert.Equal("Bo5", stored.Meta.Title);
        Assert.Equal(2, stored.Player1.Score);
    }

    [Fact]
    public async Task UpdatePlayerScore_ReportsSuccessAndBroadcastsScore()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(await MoveNextAsync(call));
        await _client.UpdateStateAsync(
            new UpdateStateRequest
            {
                State = new ScoreboardSnapshot { Player1 = new ScoreboardPlayer { Name = "Neo" } },
            },
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(await MoveNextAsync(call));

        var failed = await _client.UpdatePlayerScoreAsync(
            new UpdatePlayerScoreRequest { PlayerPosition = 0, NewScore = 5 },
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.False(failed.Success);

        var succeeded = await _client.UpdatePlayerScoreAsync(
            new UpdatePlayerScoreRequest { PlayerPosition = 1, NewScore = 7 },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var broadcast = call.ResponseStream.Current;

        Assert.True(succeeded.Success);
        Assert.Equal(ScoreboardEvent.EventOneofCase.PlayerScoreUpdated, broadcast.EventCase);
        Assert.Equal(1, broadcast.PlayerScoreUpdated.PlayerPosition);
        Assert.Equal(7, broadcast.PlayerScoreUpdated.NewScore);
    }

    [Fact]
    public async Task SetPlayerFinal_BroadcastsPlayerFinalUpdated()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(await MoveNextAsync(call));
        await _client.UpdateStateAsync(
            new UpdateStateRequest
            {
                State = new ScoreboardSnapshot
                {
                    Player2 = new ScoreboardPlayer { Name = "Morpheus" },
                },
            },
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(await MoveNextAsync(call));

        var response = await _client.SetPlayerFinalAsync(
            new SetPlayerFinalRequest { PlayerPosition = 2, Final = "winner" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var broadcast = call.ResponseStream.Current;

        Assert.True(response.Success);
        Assert.Equal(ScoreboardEvent.EventOneofCase.PlayerFinalUpdated, broadcast.EventCase);
        Assert.Equal(2, broadcast.PlayerFinalUpdated.PlayerPosition);
        Assert.Equal("winner", broadcast.PlayerFinalUpdated.Final);
    }

    [Fact]
    public async Task SetVisibility_BroadcastsVisibilityChanged()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(await MoveNextAsync(call));

        var response = await _client.SetVisibilityAsync(
            new SetVisibilityRequest { IsVisible = false },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var broadcast = call.ResponseStream.Current;

        Assert.True(response.Success);
        Assert.Equal(ScoreboardEvent.EventOneofCase.VisibilityChanged, broadcast.EventCase);
        Assert.False(broadcast.VisibilityChanged.IsVisible);
    }

    [Fact]
    public async Task GetCurrentState_ReturnsSnapshotFromService()
    {
        await _client.UpdateStateAsync(
            new UpdateStateRequest
            {
                State = new ScoreboardSnapshot
                {
                    Player1 = new ScoreboardPlayer { Name = "Trinity" },
                },
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        var response = await _client.GetCurrentStateAsync(
            new GetCurrentStateRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("Trinity", response.State.Player1.Name);
    }

    /// <summary>
    /// Таймаут обязателен: без него тест, в котором сервис не прислал событие,
    /// не падает, а висит до конца прогона и утаскивает за собой весь проект.
    /// </summary>
    private static async Task<bool> MoveNextAsync(AsyncServerStreamingCall<ScoreboardEvent> call)
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        return await call.ResponseStream.MoveNext(timeout.Token);
    }
}
