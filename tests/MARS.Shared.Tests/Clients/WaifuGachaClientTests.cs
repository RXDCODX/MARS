using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Shared.Clients;
using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Clients;

/// <summary>
/// Клиент MARS.WaifuGacha по internal API: проверяются маршруты, проброс ключа и
/// разбор конверта. Реальный сервис не нужен — HttpClient получает заглушку
/// обработчика, отвечающую заданным JSON.
/// </summary>
public class WaifuGachaClientTests
{
    [Fact]
    public async Task WaifuNameIsTakenFromEnvelope()
    {
        var handler = Stub("Аяка");
        var client = Create(handler);

        var name = await client.GetWaifuNameForUserAsync(
            "42",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Аяка", name);
        Assert.Equal(
            "/api/internal/husbands/42/waifu-name",
            handler.LastRequest.RequestUri?.AbsolutePath
        );
    }

    [Fact]
    public async Task ApiKeyHeaderIsSentWhenConfigured()
    {
        var handler = Stub("Аяка");
        var client = Create(handler, apiKey: "секретный-ключ");

        await client.GetWaifuNameForUserAsync("42", TestContext.Current.CancellationToken);

        Assert.Equal("секретный-ключ", handler.LastRequest.Headers.GetValues("X-Api-Key").Single());
    }

    [Fact]
    public async Task ApiKeyHeaderIsSkippedWhenNotConfigured()
    {
        var handler = Stub("Аяка");

        await Create(handler).GetWaifuNameForUserAsync("42", TestContext.Current.CancellationToken);

        Assert.False(handler.LastRequest.Headers.Contains("X-Api-Key"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task BlankUserIdSkipsRequest(string? userId)
    {
        var handler = Stub("Аяка");

        var name = await Create(handler)
            .GetWaifuNameForUserAsync(userId!, TestContext.Current.CancellationToken);

        Assert.Null(name);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task FailedEnvelopeYieldsNoName()
    {
        var handler = Stub("Аяка", success: false);

        var name = await Create(handler)
            .GetWaifuNameForUserAsync("42", TestContext.Current.CancellationToken);

        Assert.Null(name);
    }

    [Fact]
    public async Task ServiceErrorYieldsNoName()
    {
        var handler = Stub(HttpStatusCode.InternalServerError, "{}");

        var name = await Create(handler)
            .GetWaifuNameForUserAsync("42", TestContext.Current.CancellationToken);

        Assert.Null(name);
    }

    [Fact]
    public async Task UnreachableServiceYieldsNoName()
    {
        var handler = new ThrowingHandler(new HttpRequestException("сервис недоступен"));

        var name = await Create(handler)
            .GetWaifuNameForUserAsync("42", TestContext.Current.CancellationToken);

        Assert.Null(name);
    }

    [Fact]
    public async Task AutoHelloMessageIsRequestedWithDisplayName()
    {
        var handler = Stub("привет, pyro");

        var message = await Create(handler)
            .GetAutoHelloMessageAsync("42", "pyro", TestContext.Current.CancellationToken);

        Assert.Equal("привет, pyro", message);
        Assert.Equal("/api/internal/auto-hello/42", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.Contains("pyro", Body(handler));
    }

    [Theory]
    [InlineData("", "pyro")]
    [InlineData("42", "  ")]
    public async Task AutoHelloWithoutArgumentsSkipsRequest(string userId, string displayName)
    {
        var handler = Stub("привет");

        var message = await Create(handler)
            .GetAutoHelloMessageAsync(userId, displayName, TestContext.Current.CancellationToken);

        Assert.Null(message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task EmptyAutoHelloMessageBecomesNull()
    {
        var handler = Stub("   ");

        var message = await Create(handler)
            .GetAutoHelloMessageAsync("42", "pyro", TestContext.Current.CancellationToken);

        Assert.Null(message);
    }

    [Fact]
    public async Task AutoHelloIsToggledOn()
    {
        var handler = Stub(true);

        var enabled = await Create(handler)
            .ToggleAutoHelloAsync("42", TestContext.Current.CancellationToken);

        Assert.True(enabled);
        Assert.Equal(
            "/api/internal/auto-hello/42/toggle",
            handler.LastRequest.RequestUri?.AbsolutePath
        );
        Assert.Contains("true", Body(handler));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ToggleWithoutUserIdSkipsRequest(string userId)
    {
        var handler = Stub(true);

        var enabled = await Create(handler)
            .ToggleAutoHelloAsync(userId, TestContext.Current.CancellationToken);

        Assert.False(enabled);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task InventoriesAreRequestedForBothKinds()
    {
        var handler = Stub(new CollectionInventory(2, 5, []));
        var client = Create(handler);

        var fumo = await client.GetFumoInventoryAsync("42", TestContext.Current.CancellationToken);
        var miku = await client.GetMikuInventoryAsync("42", TestContext.Current.CancellationToken);

        Assert.Equal(2, fumo!.Collected);
        Assert.Equal(5, miku!.Total);
        Assert.Equal(
            ["/api/internal/collections/fumo", "/api/internal/collections/miku"],
            handler.Requests.Select(request => request.RequestUri!.AbsolutePath).ToArray()
        );
    }

    [Fact]
    public async Task RandomTitlesAreRequestedFromShikimoriEndpoints()
    {
        var handler = Stub(
            new ShikimoriTitleRef(7, "Название", "Title", 2020, "https://shikimori.one/animes/7")
        );
        var client = Create(handler);

        var anime = await client.GetRandomAnimeAsync(TestContext.Current.CancellationToken);
        var manga = await client.GetRandomMangaAsync(TestContext.Current.CancellationToken);

        Assert.Equal(7, anime!.Id);
        Assert.Equal(7, manga!.Id);
        Assert.Equal(
            ["/api/internal/shikimori/random-anime", "/api/internal/shikimori/random-manga"],
            handler.Requests.Select(request => request.RequestUri!.AbsolutePath).ToArray()
        );
    }

    [Fact]
    public async Task MissingTitleBecomesNull()
    {
        var handler = Stub((string?)null);

        Assert.Null(
            await Create(handler).GetRandomAnimeAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UserIdIsEscapedInPath()
    {
        var handler = Stub("Аяка");

        await Create(handler)
            .GetWaifuNameForUserAsync("id with/slash", TestContext.Current.CancellationToken);

        Assert.Equal(
            "/api/internal/husbands/id%20with%2Fslash/waifu-name",
            handler.LastRequest.RequestUri!.AbsolutePath
        );
    }

    [Fact]
    public void ServiceEndpointComesFromConfiguration()
    {
        var client = Create(
            Stub("Аяка"),
            endpoints: new ServiceEndpoints { WaifuGacha = "http://waifu-gacha:9999" }
        );

        Assert.Equal("http://waifu-gacha:9999", client.ServiceEndpoint);
    }

    [Fact]
    public async Task CanceledTokenIsNotSwallowed()
    {
        var handler = Stub("Аяка");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Create(handler).GetWaifuNameForUserAsync("42", cancellation.Token)
        );
    }

    /// <summary>
    /// Конверт в том виде, в каком его пишут сервисы (enum <c>statusCode</c> числом,
    /// а не строкой), обязан разбираться клиентом: иначе любой успешный ответ
    /// превратился бы в null и вызывающий решил бы, что данных нет.
    /// </summary>
    [Fact]
    public async Task EnvelopeWrittenByServicesIsParsed()
    {
        var handler = new StubHandler(
            """{"success":true,"result":"Ayaka","errorMessage":null,"statusCode":200}"""
        );

        var name = await Create(handler)
            .GetWaifuNameForUserAsync("42", TestContext.Current.CancellationToken);

        Assert.Equal("Ayaka", name);
    }

    private static WaifuGachaClient Create(
        HttpMessageHandler handler,
        string? apiKey = null,
        ServiceEndpoints? endpoints = null
    ) =>
        new(
            new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://waifu-gacha:8080"),
            },
            Options.Create(new ServiceAuthOptions { ApiKey = apiKey }),
            Options.Create(endpoints ?? new ServiceEndpoints()),
            NullLogger<WaifuGachaClient>.Instance
        );

    /// <summary>
    /// Конверт пишется теми же настройками, что и MVC сервисов: enum'ы числами.
    /// </summary>
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

    private static StubHandler Stub(string result, bool success = true) =>
        new(Envelope(result, success));

    private static StubHandler Stub<T>(T result) => new(Envelope(result));

    private static StubHandler Stub(HttpStatusCode status, string body) => new(body, status);

    private static string Body(StubHandler handler) => handler.SentBody ?? string.Empty;

    private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public string? SentBody { get; private set; }

        public HttpRequestMessage LastRequest => Requests[^1];

        public List<string> Bodies { get; } = [];

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
