using System.Net;
using System.Net.Http.Json;
using MARS.OBS.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.OBS.Tests.Services;

/// <summary>
/// Обращения к OBS идут через локальный http-мост AudioController, а не напрямую
/// в websocket: тесты проверяют разбор ответов этого моста. Реальный OBS не
/// поднимается — HttpClient получает заглушку обработчика.
/// </summary>
public class HttpObsServiceTests
{
    [Fact]
    public async Task ConnectRefreshesStatusFromBridge()
    {
        var handler = new RecordingHandler(request =>
            Json("""{"isConnected":true,"isPaused":false}""")
        );
        var service = Create(handler);

        await service.ConnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal("POST", handler.Requests[0].Method);
        Assert.Equal("/api/obs/connect", handler.Requests[0].Path);
        Assert.Equal("GET", handler.Requests[1].Method);
        Assert.Equal("/api/obs/status", handler.Requests[1].Path);
        Assert.True(service.IsConnected);
        Assert.False(service.IsPaused);
    }

    [Fact]
    public async Task ConnectUsesLocalhostBridge()
    {
        var handler = new RecordingHandler(request =>
            Json("""{"isConnected":true,"isPaused":false}""")
        );

        await Create(handler).ConnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal("localhost", handler.Requests[0].Host);
    }

    /// <summary>
    /// Отказ моста обязан быть виден вызывающему: подключение «вроде удалось»,
    /// при котором OBS не отвечает, хуже явной ошибки.
    /// </summary>
    [Fact]
    public async Task ConnectRethrowsBridgeFailure()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var service = Create(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.ConnectAsync(TestContext.Current.CancellationToken)
        );
        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task DisconnectRefreshesStatus()
    {
        var handler = new RecordingHandler(request =>
            request.Path.Contains("status", StringComparison.Ordinal)
                ? Json("""{"isConnected":false,"isPaused":false}""")
                : Json("""{}""")
        );
        var service = Create(handler);

        await service.DisconnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal("/api/obs/disconnect", handler.Requests[0].Path);
        Assert.False(service.IsConnected);
    }

    /// <summary>
    /// Разрыв при отключении не должен поднимать исключение: выключение идёт в
    /// <c>finally</c>-подобных местах, и падение там сорвало бы остановку сервиса.
    /// </summary>
    [Fact]
    public async Task DisconnectFailureIsLoggedAndIgnored()
    {
        var handler = new RecordingHandler(_ => throw new HttpRequestException("bridge down"));
        var service = Create(handler);

        await service.DisconnectAsync(TestContext.Current.CancellationToken);

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ScreenshotReturnsPathFromBridge()
    {
        var handler = new RecordingHandler(_ => Json("""{"screenshotPath":"/shots/frame.png"}"""));
        var service = Create(handler);

        var path = await service.ScreenshotAsync("Микрофон", TestContext.Current.CancellationToken);

        Assert.Equal("/shots/frame.png", path);
        Assert.Equal(
            "/api/obs/screenshot?sourceName=Микрофон",
            Uri.UnescapeDataString(handler.Requests[0].Path)
        );
    }

    [Fact]
    public async Task ScreenshotWithoutSourceSendsEmptyParameter()
    {
        var handler = new RecordingHandler(_ => Json("""{"screenshotPath":null}"""));
        var service = Create(handler);

        var path = await service.ScreenshotAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, path);
        Assert.Equal("/api/obs/screenshot?sourceName=", handler.Requests[0].Path);
    }

    [Fact]
    public async Task ScreenshotWithoutBodyReturnsEmptyPath()
    {
        var handler = new RecordingHandler(_ => Json("null"));
        var service = Create(handler);

        Assert.Equal(
            string.Empty,
            await service.ScreenshotAsync(null, TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [InlineData("freeze", true)]
    [InlineData("unfreeze", false)]
    [InlineData("pause-scene", true)]
    [InlineData("unpause-scene", false)]
    public async Task PauseCommandsReturnBridgeState(string command, bool expected)
    {
        var handler = new RecordingHandler(request =>
            request.Path.Contains("status", StringComparison.Ordinal)
                ? Json($$"""{"isConnected":true,"isPaused":{{(expected ? "true" : "false")}}}""")
                : Json($$"""{"success":true,"isPaused":{{(expected ? "true" : "false")}}}""")
        );
        var service = Create(handler);

        var result = command switch
        {
            "freeze" => await service.FreezeAsync(TestContext.Current.CancellationToken),
            "unfreeze" => await service.UnfreezeAsync(TestContext.Current.CancellationToken),
            "pause-scene" => await service.SwitchToPauseSceneAsync(
                TestContext.Current.CancellationToken
            ),
            _ => await service.SwitchFromPauseSceneAsync(TestContext.Current.CancellationToken),
        };

        Assert.True(result.Success, result.Error);
        Assert.Equal(expected, result.IsPaused);
        Assert.Equal($"/api/obs/{command}", handler.Requests[0].Path);
        Assert.Equal(expected, service.IsPaused);
        Assert.True(service.IsConnected);
    }

    [Theory]
    [InlineData(ObsPauseMode.FreezeFrame)]
    [InlineData(ObsPauseMode.PauseScene)]
    public async Task ToggleSendsModeAsQueryParameter(ObsPauseMode mode)
    {
        var handler = new RecordingHandler(request =>
            request.Path.Contains("status", StringComparison.Ordinal)
                ? Json("""{"isConnected":true,"isPaused":false}""")
                : Json("""{"success":true,"isPaused":true}""")
        );
        var service = Create(handler);

        var result = await service.TogglePauseAsync(mode, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(result.IsPaused);
        Assert.Equal($"/api/obs/toggle?mode={(int)mode}", handler.Requests[0].Path);
    }

    [Fact]
    public async Task ToggleDefaultsToFreezeFrameMode()
    {
        var handler = new RecordingHandler(_ => Json("""{"success":true,"isPaused":true}"""));

        await Create(handler)
            .TogglePauseAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            $"/api/obs/toggle?mode={(int)ObsPauseMode.FreezeFrame}",
            handler.Requests[0].Path
        );
    }

    [Fact]
    public async Task PauseResultCarriesScreenshotPath()
    {
        var handler = new RecordingHandler(_ =>
            Json("""{"success":true,"isPaused":true,"screenshotPath":"/s.png"}""")
        );

        var result = await Create(handler).FreezeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("/s.png", result.ScreenshotPath);
    }

    [Fact]
    public async Task UnsuccessfulCommandReturnsBridgeError()
    {
        var handler = new RecordingHandler(_ =>
            Json("""{"success":false,"error":"сцена не найдена"}""")
        );

        var result = await Create(handler).FreezeAsync(TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("сцена не найдена", result.Error);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task UnsuccessfulCommandWithoutErrorTextReportsUnknown()
    {
        var handler = new RecordingHandler(_ => Json("""{"success":false}"""));

        var result = await Create(handler).UnfreezeAsync(TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Unknown error", result.Error);
    }

    [Fact]
    public async Task NetworkFailureBecomesFailedResult()
    {
        var handler = new RecordingHandler(_ => throw new HttpRequestException("мост недоступен"));

        var result = await Create(handler).FreezeAsync(TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("мост недоступен", result.Error);
    }

    /// <summary>
    /// Статус не обновляется, когда мост не ответил: иначе панель показала бы
    /// «пауза снята» после неудачной команды.
    /// </summary>
    [Fact]
    public async Task FailedStatusRefreshKeepsPreviousState()
    {
        var handler = new RecordingHandler(request =>
            request.Path.Contains("status", StringComparison.Ordinal)
                ? throw new HttpRequestException("статус недоступен")
                : Json("""{"success":true,"isPaused":true}""")
        );
        var service = Create(handler);

        var result = await service.FreezeAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.False(service.IsConnected);
        Assert.False(service.IsPaused);
    }

    [Fact]
    public async Task EmptyStatusBodyKeepsPreviousState()
    {
        var handler = new RecordingHandler(_ => Json("null"));

        await Create(handler).ConnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.ResponseCount);
        Assert.False(handler.Requests[1].Path.Contains("null", StringComparison.Ordinal));
    }

    private static HttpObsService Create(RecordingHandler handler, bool development = false)
    {
        var environment = new Mock<IHostEnvironment>();
        environment
            .Setup(instance => instance.EnvironmentName)
            .Returns(development ? "Development" : "Production");
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(instance => instance.CreateClient("ObsConnector"))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        return new HttpObsService(
            factory.Object,
            NullLogger<HttpObsService>.Instance,
            environment.Object
        );
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };

    /// <summary>
    /// Заглушка без сети: фиксирует маршруты и отдаёт заданные ответы, чтобы
    /// проверялся разбор ответа моста, а не сам OBS.
    /// </summary>
    private sealed class RecordingHandler(Func<RecordedRequest, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        public int ResponseCount => Requests.Count;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var recorded = new RecordedRequest(
                request.Method.Method,
                request.RequestUri?.PathAndQuery ?? string.Empty,
                request.RequestUri?.Host ?? string.Empty
            );
            Requests.Add(recorded);

            var response = responder(recorded);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    private sealed record RecordedRequest(string Method, string Path, string Host);
}
