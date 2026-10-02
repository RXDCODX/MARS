using System.Net;
using MARS.Shared.Clients;
using MARS.Shared.Configuration;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Clients;

/// <summary>
/// Межсервисные клиенты MARS.Shared поверх заглушки HTTP: проверяются маршруты,
/// проброс ключа и разбор конверта. Реальные сервисы не поднимаются — клиент
/// получает заглушку обработчика, поэтому проверяется логика клиента, а не
/// доступность Docker-сети.
/// </summary>
public class ServiceClientsTests
{
    [Fact]
    public async Task ShikimoriReturnsRandomAnime()
    {
        var handler = Stub(
            Envelope(
                new ShikimoriTitleRef(
                    7,
                    "Название",
                    "Русское",
                    2020,
                    "https://shikimori.one/animes/7"
                )
            )
        );
        var client = new ShikimoriApiClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<ShikimoriApiClient>.Instance
        );

        var anime = await client.GetRandomAnimeAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(anime);
        Assert.Equal(7, anime!.Id);
        Assert.Equal("/api/Shikimori/random-anime", handler.LastPath);
        Assert.Equal("http://shikimori:8080", client.ServiceEndpoint);
    }

    [Fact]
    public async Task ShikimoriReturnsRandomManga()
    {
        var handler = Stub(
            Envelope(
                new ShikimoriTitleRef(8, "Манга", null, null, "https://shikimori.one/mangas/8")
            )
        );
        var client = new ShikimoriApiClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<ShikimoriApiClient>.Instance
        );

        await client.GetRandomMangaAsync(TestContext.Current.CancellationToken);

        Assert.Equal("/api/Shikimori/random-manga", handler.LastPath);
    }

    [Fact]
    public async Task ShikimoriReturnsCharacterById()
    {
        var handler = Stub(
            Envelope(
                new ShikimoriCharacterRef(
                    42,
                    "Персонаж",
                    null,
                    null,
                    "https://shikimori.one/characters/42",
                    "characters/42.jpg",
                    null,
                    null
                )
            )
        );
        var client = new ShikimoriApiClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<ShikimoriApiClient>.Instance
        );

        var character = await client.GetCharacterAsync(42, TestContext.Current.CancellationToken);

        Assert.NotNull(character);
        Assert.Equal("/api/Shikimori/characters/42", handler.LastPath);
    }

    [Fact]
    public async Task ShikimoriReturnsRateLimiterState()
    {
        var handler = Stub(Envelope(new ShikimoriRateLimiterInfo(5, 300, 1.5, 30.5)));
        var client = new ShikimoriApiClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<ShikimoriApiClient>.Instance
        );

        var info = await client.GetRateLimiterInfoAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(info);
        Assert.Equal(300, info!.AvailablePerMinute);
        Assert.Equal("/api/Shikimori/rate-limiter", handler.LastPath);
    }

    [Fact]
    public async Task LeaderboardReturnsTop()
    {
        var handler = Stub(
            Envelope(new LeaderboardTop([new LeaderboardEntry("123456789", "Pyro", 42, 1, 2)]))
        );
        var client = new LeaderboardClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<LeaderboardClient>.Instance
        );

        var top = await client.GetTopAsync(5, TestContext.Current.CancellationToken);

        Assert.NotNull(top);
        Assert.NotNull(top!.Entries);
        Assert.Equal(42, top.Entries[0].TotalWins);
        Assert.Equal("/api/leaderboard/top?count=5", handler.LastPath);
        Assert.Equal("http://twitch-core:8080", client.ServiceEndpoint);
    }

    [Fact]
    public async Task LeaderboardReturnsUserStats()
    {
        var handler = Stub(
            Envelope(new LeaderboardStats(1, new LeaderboardEntry("123456789", "Pyro", 42, 0, 0)))
        );
        var client = new LeaderboardClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<LeaderboardClient>.Instance
        );

        var stats = await client.GetUserStatsAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(stats);
        Assert.Equal(1, stats!.Place);
        Assert.Equal("/api/leaderboard/user/123456789", handler.LastPath);
    }

    [Fact]
    public async Task MediaStorageReturnsSingleMediaInfo()
    {
        var id = Guid.NewGuid();
        var handler = Stub(Envelope(MediaInfo(id, "клип.mp4", "mp4")));
        var client = new MediaStorageClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<MediaStorageClient>.Instance
        );

        var info = await client.GetMediaInfoAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(info);
        Assert.Equal("клип.mp4", info!.FileInfo.FileName);
        Assert.Equal($"/api/MediaInfo/{id}", handler.LastPath);
        Assert.Equal("http://media-storage:8080", client.ServiceEndpoint);
    }

    [Fact]
    public async Task MediaStorageReturnsAlertsList()
    {
        var handler = Stub(Envelope<List<MediaInfo>>([MediaInfo(Guid.NewGuid(), "а.mp3", "mp3")]));
        var client = new MediaStorageClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<MediaStorageClient>.Instance
        );

        var alerts = await client.GetAllAlertsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(alerts);
        Assert.Single(alerts!);
        Assert.Equal("/api/MediaInfo", handler.LastPath);
    }

    [Fact]
    public async Task TwitchUserIsResolvedByLogin()
    {
        var handler = Stub(Envelope("123456789"));
        var client = new TwitchUserClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<TwitchUserClient>.Instance
        );

        var id = await client.ResolveIdByLoginAsync(
            "  @pyro  ",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("123456789", id);
        Assert.Equal("/api/TwitchUsers/by-login/pyro", handler.LastPath);
    }

    [Fact]
    public async Task DiscordMessageIsAccepted()
    {
        var handler = Stub(Envelope(true));
        var client = new DiscordClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<DiscordClient>.Instance
        );

        Assert.True(
            await client.SendMessageAsync(42UL, "привет", TestContext.Current.CancellationToken)
        );
        Assert.Equal(
            "/api/Discord/send?channelId=42&message=%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82",
            handler.LastPath
        );
        Assert.Equal("http://discord:8080", client.ServiceEndpoint);
    }

    [Fact]
    public async Task DiscordRejectionIsReportedAsNotSent()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":false,"errorMessage":"канал не найден"}""",
                System.Text.Encoding.UTF8,
                "application/json"
            ),
        });
        var client = new DiscordClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<DiscordClient>.Instance
        );

        Assert.False(
            await client.SendMessageAsync(42UL, "привет", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task DiscordHttpErrorIsReportedAsNotSent()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var client = new DiscordClient(
            Http(handler),
            Auth(),
            Endpoints(),
            NullLogger<DiscordClient>.Instance
        );

        Assert.False(
            await client.SendMessageAsync(42UL, "привет", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task DiscordNetworkFailureIsReportedAsNotSent()
    {
        var client = new DiscordClient(
            Http(new ThrowingHandler(new HttpRequestException("discord недоступен"))),
            Auth(),
            Endpoints(),
            NullLogger<DiscordClient>.Instance
        );

        Assert.False(
            await client.SendMessageAsync(42UL, "привет", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task CanceledRequestIsNotSwallowed()
    {
        var client = new TwitchUserClient(
            Http(Stub(Envelope("\"1\""))),
            Auth(),
            Endpoints(),
            NullLogger<TwitchUserClient>.Instance
        );
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.ResolveIdByLoginAsync("pyro", cancellation.Token)
        );
    }

    private static MediaInfo MediaInfo(Guid id, string fileName, string extension) =>
        new()
        {
            Id = id,
            TextInfo = new MediaTextInfo { Text = fileName },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "wwwroot/" + fileName,
                FileName = fileName,
                Extension = extension,
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = fileName },
            StylesInfo = new MediaStylesInfo(),
        };

    private static HttpClient Http(HttpMessageHandler handler) =>
        new(handler, disposeHandler: false) { BaseAddress = new Uri("http://service:8080") };

    private static IOptions<ServiceAuthOptions> Auth(string? apiKey = "ключ") =>
        Options.Create(new ServiceAuthOptions { ApiKey = apiKey });

    private static IOptions<ServiceEndpoints> Endpoints() => Options.Create(new ServiceEndpoints());

    private static StubHandler Stub(string body) =>
        new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });

    private static string Envelope<T>(T value) =>
        System.Text.Json.JsonSerializer.Serialize(
            new
            {
                success = true,
                result = value,
                statusCode = 200,
            },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        );

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public string LastPath { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastPath = request.RequestUri?.AbsolutePath + request.RequestUri?.Query;

            var response = responder(request);
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
