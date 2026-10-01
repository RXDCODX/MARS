using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Services;
using MARS.Shared.Grpc.Tuna;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TunaServiceClient = MARS.Shared.Grpc.Tuna.TunaService.TunaServiceClient;

namespace MARS.Shared.Tests.Grpc;

public class TunaGrpcServiceTests : IAsyncLifetime
{
    private static readonly TimeSpan StreamReadTimeout = TimeSpan.FromSeconds(10);

    private readonly GrpcEventBroadcaster<TunaEvent> _broadcaster = new();
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private TunaServiceClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_broadcaster);

        _app = builder.Build();
        _app.MapGrpcService<TunaGrpcService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _channel = GrpcChannel.ForAddress(
            "http://alerts.test",
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() }
        );
        _client = new TunaServiceClient(_channel);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task SendPlayerData_DeliversStateToSubscribers()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _client.SendPlayerDataAsync(
            new SendPlayerDataRequest { Info = CreateInfo("Bad Apple!!") },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var event_ = call.ResponseStream.Current;

        Assert.Equal("Bad Apple!!", event_.Info.Data.Title);
        Assert.Equal("Ren", event_.Info.Hostname);
        Assert.Equal(["tnamatsukage"], event_.Info.Data.Artists);
    }

    [Fact]
    public async Task Subscribe_SkipsStateThatWasSentBeforeItSubscribed()
    {
        await _client.SendPlayerDataAsync(
            new SendPlayerDataRequest { Info = CreateInfo("Yellow Submarine") },
            cancellationToken: TestContext.Current.CancellationToken
        );

        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _client.SendPlayerDataAsync(
            new SendPlayerDataRequest { Info = CreateInfo("Bohemian Rhapsody") },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));

        Assert.Equal("Bohemian Rhapsody", call.ResponseStream.Current.Info.Data.Title);
    }

    [Fact]
    public async Task BeYm_SecondCallIsRejected()
    {
        var first = await _client.BeYmAsync(
            new BeYmRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        var failure = await Assert.ThrowsAsync<RpcException>(
            async () =>
                await _client.BeYmAsync(
                    new BeYmRequest(),
                    cancellationToken: TestContext.Current.CancellationToken
                )
        );

        Assert.NotNull(first);
        Assert.Equal(StatusCode.FailedPrecondition, failure.StatusCode);
    }

    private static TunaPayload CreateInfo(string title)
    {
        var track = new TunaTrack
        {
            Id = Guid.NewGuid().ToString(),
            Cover = "https://cover",
            Title = title,
            Status = "playing",
            Progression = 12,
            Duration = 120,
            AlbumUrl = "https://album",
        };
        track.Artists.Add("tnamatsukage");

        return new TunaPayload
        {
            Data = track,
            Hostname = "Ren",
            Timestamp = "2026-01-01T00:00:00",
        };
    }

    private static async Task<bool> MoveNextAsync(AsyncServerStreamingCall<TunaEvent> call)
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        return await call.ResponseStream.MoveNext(timeout.Token);
    }

    private async Task WaitForSubscriberAsync()
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        while (_broadcaster.SubscriberCount == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }
}
