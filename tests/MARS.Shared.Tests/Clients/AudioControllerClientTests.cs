using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Clients;
using MARS.Shared.Configuration;
using MARS.Shared.Models.Audio;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MARS.Shared.Tests.Clients;

/// <summary>
/// Клиент MARS.AudioController по internal API: маршруты, ключ и разбор конверта.
/// Реальный сервис не нужен — HttpClient получает заглушку обработчика.
/// </summary>
public class AudioControllerClientTests
{
    [Fact]
    public async Task MutePostsToMixerRoute()
    {
        var handler = Stub(
            new MuteMixerResponse { MutedSessionCount = 3, MutedProcessNames = ["steam"] }
        );

        var response = await Create(handler)
            .MuteAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(3, response!.MutedSessionCount);
        Assert.Equal(["steam"], response.MutedProcessNames);
        Assert.Equal("/api/Mixer/mute", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
    }

    [Fact]
    public async Task MutePassesExcludedProcessNamesInBody()
    {
        var handler = Stub(new MuteMixerResponse());

        await Create(handler)
            .MuteAsync(["obs64", "obs-browser-page"], TestContext.Current.CancellationToken);

        Assert.Contains("obs64", Body(handler));
        Assert.Contains("obs-browser-page", Body(handler));
    }

    [Fact]
    public async Task UnmutePostsToMixerRoute()
    {
        var handler = Stub(new UnmuteMixerResponse { UnmutedSessionCount = 2 });

        var response = await Create(handler).UnmuteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, response!.UnmutedSessionCount);
        Assert.Equal("/api/Mixer/unmute", handler.LastRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task MutedSessionsAreReadFromMixerRoute()
    {
        var handler = Stub(
            new MutedSessionsResponse
            {
                Sessions = [new MutedSessionDto { ProcessId = 7, ProcessName = "steam" }],
            }
        );

        var response = await Create(handler)
            .GetMutedSessionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal("steam", response!.Sessions[0].ProcessName);
        Assert.Equal("/api/Mixer/muted", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
    }

    [Fact]
    public async Task MicrophoneVolumeIsReadFromRoute()
    {
        var handler = Stub(new MicrophoneVolumeResponse { Volume = 0.7f, IsMuted = false });

        var response = await Create(handler)
            .GetMicrophoneVolumeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0.7f, response!.Volume);
        Assert.Equal("/api/Microphone/volume", handler.LastRequest.RequestUri?.AbsolutePath);
    }

    /// <summary>
    /// Громкость в пути пишется десятичной точкой, а не текущей локалью: маршрутное
    /// ограничение <c>:float</c> разбирается инвариантной культурой, и «0,5» ушло бы в 404.
    /// </summary>
    [Theory]
    [InlineData(0.5f, "0.5")]
    [InlineData(0.05f, "0.05")]
    public async Task MicrophoneVolumeIsSetWithInvariantDecimalSeparator(
        float volume,
        string expectedSegment
    )
    {
        var handler = Stub(new MicrophoneVolumeSetResponse { CurrentVolume = volume });

        await Create(handler)
            .SetMicrophoneVolumeAsync(volume, TestContext.Current.CancellationToken);

        Assert.Equal(
            $"/api/Microphone/volume/{expectedSegment}",
            handler.LastRequest.RequestUri?.AbsolutePath
        );
    }

    [Fact]
    public async Task MicrophoneVolumeToMaxPostsToRoute()
    {
        var handler = Stub(new MicrophoneVolumeSetResponse { CurrentVolume = 1f });

        var response = await Create(handler)
            .SetMicrophoneVolumeToMaxAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1f, response!.CurrentVolume);
        Assert.Equal("/api/Microphone/volume/max", handler.LastRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ApiKeyHeaderIsSentWhenConfigured()
    {
        var handler = Stub(new MuteMixerResponse());

        await Create(handler, apiKey: "секретный-ключ")
            .MuteAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("секретный-ключ", handler.LastRequest.Headers.GetValues("X-Api-Key").Single());
    }

    [Fact]
    public async Task FailedEnvelopeYieldsNoResult()
    {
        var handler = Stub(new MuteMixerResponse(), success: false);

        var response = await Create(handler)
            .MuteAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task ServiceErrorYieldsNoResult()
    {
        var handler = Stub(HttpStatusCode.InternalServerError, "{}");

        var response = await Create(handler)
            .MuteAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task UnreachableServiceYieldsNoResult()
    {
        var handler = new ThrowingHandler(new HttpRequestException("сервис недоступен"));

        var response = await Create(handler)
            .MuteAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(response);
    }

    [Fact]
    public async Task CanceledTokenIsNotSwallowed()
    {
        var handler = Stub(new MuteMixerResponse());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Create(handler).MuteAsync(cancellationToken: cancellation.Token)
        );
    }

    [Fact]
    public void ServiceEndpointComesFromConfiguration()
    {
        var client = Create(Stub(new MuteMixerResponse()), baseUrl: "http://audio-host:30695");

        Assert.Equal("http://audio-host:30695", client.ServiceEndpoint);
    }

    private static AudioControllerClient Create(
        HttpMessageHandler handler,
        string? apiKey = null,
        string? baseUrl = null
    ) =>
        new(
            new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri(baseUrl ?? "http://audio-controller:30695"),
            },
            Options.Create(new ServiceAuthOptions { ApiKey = apiKey }),
            Options.Create(
                new AudioControllerConfiguration
                {
                    BaseUrl = baseUrl ?? "http://audio-controller:30695",
                }
            ),
            NullLogger<AudioControllerClient>.Instance
        );

    private static string Envelope<T>(T value, bool success = true, string? error = null) =>
        JsonSerializer.Serialize(
            new
            {
                success,
                result = value,
                errorMessage = error,
                statusCode = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );

    private static StubHandler Stub<T>(T result, bool success = true) =>
        new(Envelope(result, success));

    private static StubHandler Stub(HttpStatusCode status, string body) => new(body, status);

    private static string Body(StubHandler handler) => handler.SentBody ?? string.Empty;

    private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public string? SentBody { get; private set; }

        public HttpRequestMessage LastRequest => Requests[^1];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            SentBody = request
                .Content?.ReadAsStringAsync(TestContext.Current.CancellationToken)
                .GetAwaiter()
                .GetResult();
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
            response.RequestMessage = request;

            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => throw exception;
    }
}
