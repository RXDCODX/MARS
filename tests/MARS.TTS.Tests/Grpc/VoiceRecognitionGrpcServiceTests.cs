using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Voice;
using MARS.TTS.Grpc;
using MARS.TTS.Models;
using MARS.TTS.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwitchUser = MARS.TTS.Models.TwitchUser;
using VoiceRecognitionServiceClient = MARS.Shared.Grpc.Voice.VoiceRecognitionService.VoiceRecognitionServiceClient;

namespace MARS.TTS.Tests.Grpc;

public class VoiceRecognitionGrpcServiceTests : IAsyncLifetime
{
    private static readonly TimeSpan StreamReadTimeout = TimeSpan.FromSeconds(10);

    private readonly FakeTtsMessageFilterService _filter = new();
    private readonly GrpcEventBroadcaster<VoiceEvent> _broadcaster = new();
    private ITtsNotifier _notifier = null!;
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private VoiceRecognitionServiceClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<ITtsMessageFilterService>(_filter);
        builder.Services.AddSingleton(_broadcaster);
        builder.Services.AddSingleton<ITtsNotifier, TtsNotifier>();

        _app = builder.Build();
        _app.MapGrpcService<VoiceRecognitionGrpcService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _notifier = _app.Services.GetRequiredService<ITtsNotifier>();
        _channel = GrpcChannel.ForAddress(
            "http://tts.test",
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() }
        );
        _client = new VoiceRecognitionServiceClient(_channel);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task BroadcastAsync_DeliversPlayTtsToSubscribers()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _notifier.BroadcastAsync(
            new TwitchUser
            {
                TwitchId = "42",
                UserLogin = "pyro",
                DisplayName = "Pyro",
                AliasNickname = "Пиро",
                ChatColor = "#ff0000",
            },
            "привет чат",
            TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var event_ = call.ResponseStream.Current;

        Assert.Equal(VoiceEvent.EventOneofCase.PlayTts, event_.EventCase);
        Assert.Equal("привет чат", event_.PlayTts.Message);
        Assert.Equal("Пиро", event_.PlayTts.User.DisplayName);
        Assert.Equal("pyro", event_.PlayTts.User.UserLogin);
        Assert.Equal("#ff0000", event_.PlayTts.User.ChatColor);
    }

    [Fact]
    public async Task BroadcastAsync_SkipsMessageWhenFilterRejectsIt()
    {
        _filter.IsFilterEnabled = true;
        _filter.RejectedText = "мусор";
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _notifier.BroadcastAsync(
            new TwitchUser
            {
                TwitchId = "42",
                UserLogin = "pyro",
                DisplayName = "Pyro",
            },
            "мусор",
            TestContext.Current.CancellationToken
        );

        await _notifier.BroadcastAsync(
            new TwitchUser
            {
                TwitchId = "43",
                UserLogin = "neo",
                DisplayName = "Neo",
            },
            "чистое сообщение",
            TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));

        Assert.Equal("чистое сообщение", call.ResponseStream.Current.PlayTts.Message);
    }

    [Fact]
    public async Task BroadcastStateAsync_BroadcastsStateAndClampsTrackedVolume()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _notifier.BroadcastStateAsync(
            new TtsState { IsStopped = true, Volume = 5 },
            TestContext.Current.CancellationToken
        );

        Assert.True(await MoveNextAsync(call));
        var event_ = call.ResponseStream.Current;

        Assert.Equal(VoiceEvent.EventOneofCase.UpdateTtsState, event_.EventCase);
        Assert.True(event_.UpdateTtsState.IsStopped);
        Assert.Equal(5.0, event_.UpdateTtsState.Volume);
        Assert.Equal(2.0, _notifier.CurrentVolume);
    }

    [Fact]
    public async Task BroadcastReassignVoiceAsync_DeliversUserId()
    {
        using var call = _client.Subscribe(
            new SubscribeRequest(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        await WaitForSubscriberAsync();

        await _notifier.BroadcastReassignVoiceAsync("42", TestContext.Current.CancellationToken);

        Assert.True(await MoveNextAsync(call));
        var event_ = call.ResponseStream.Current;

        Assert.Equal(VoiceEvent.EventOneofCase.ReassignVoice, event_.EventCase);
        Assert.Equal("42", event_.ReassignVoice.UserId);
    }

    [Fact]
    public async Task PlaybackReports_AreAccepted()
    {
        await _client.ReportTtsPlaybackStartedAsync(
            new ReportTtsPlaybackStartedRequest { Text = "привет" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        var completed = await _client.ReportTtsPlaybackCompletedAsync(
            new ReportTtsPlaybackCompletedRequest { Text = "привет", DurationSeconds = 3 },
            cancellationToken: TestContext.Current.CancellationToken
        );

        var failed = await _client.ReportTtsPlaybackFailedAsync(
            new ReportTtsPlaybackFailedRequest { Text = "привет", Error = "no audio device" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.NotNull(completed);
        Assert.NotNull(failed);
    }

    private static async Task<bool> MoveNextAsync(AsyncServerStreamingCall<VoiceEvent> call)
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        return await call.ResponseStream.MoveNext(timeout.Token);
    }

    /// <summary>
    /// gRPC-клиент отправляет запрос лениво, при первом чтении потока: без этой
    /// паузы рассылка ушла бы в пустоту, и тест был бы плавающим.
    /// </summary>
    private async Task WaitForSubscriberAsync()
    {
        using var timeout = new CancellationTokenSource(StreamReadTimeout);

        while (_broadcaster.SubscriberCount == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }
}
