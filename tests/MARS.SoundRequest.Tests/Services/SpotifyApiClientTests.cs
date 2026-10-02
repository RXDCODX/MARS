using System.Net;
using System.Text.Json;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.Spotify;
using MARS.SoundRequest.Tests.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Клиент Spotify поверх заглушки HTTP и настоящего хранилища учётных данных в
/// памяти: токен берётся из RootState без обращения к accounts.spotify.com,
/// поэтому проверяются маршруты, разбор ответа и выбор устройства.
///
/// Сеть не нужна: настоящий Spotify здесь не запустится, а проверяется разбор его
/// ответов.
/// </summary>
public class SpotifyApiClientTests : IDisposable
{
    private const string AccessToken = "тестовый-токен";

    private readonly TestDbContextFactory _factory;
    private readonly StubHandler _handler = new();
    private readonly SpotifyApiClient _client;

    public SpotifyApiClientTests()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"spotify-api-{Guid.NewGuid():N}")
            .Options;

        _factory = new TestDbContextFactory(options);
        using (var database = _factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
            Seed(database, withDeviceId: false);
        }

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));

        var auth = new SpotifyAuthService(
            _factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );

        _client = new SpotifyApiClient(
            new HttpClient(_handler, disposeHandler: false),
            auth,
            Options.Create(
                new SpotifySoundRequestConfiguration
                {
                    Enabled = true,
                    Market = "RU",
                    ForceDeviceTransfer = false,
                }
            ),
            NullLogger<SpotifyApiClient>.Instance
        );
    }

    public void Dispose()
    {
        using var database = _factory.CreateDbContext();
        database.Database.EnsureDeleted();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfiguredFlagFollowsSettings(bool enabled)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"spotify-flag-{Guid.NewGuid():N}")
            .Options;
        var factory = new TestDbContextFactory(options);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
        var auth = new SpotifyAuthService(
            factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );
        var client = new SpotifyApiClient(
            new HttpClient(_handler, disposeHandler: false),
            auth,
            Options.Create(new SpotifySoundRequestConfiguration { Enabled = enabled }),
            NullLogger<SpotifyApiClient>.Instance
        );

        Assert.Equal(enabled, client.IsConfigured());
    }

    [Theory]
    [InlineData("spotify:track:4uLU6hMCjMI75M1A2tKUQC", "4uLU6hMCjMI75M1A2tKUQC")]
    [InlineData("https://open.spotify.com/track/4uLU6hMCjMI75M1A2tKUQC", "4uLU6hMCjMI75M1A2tKUQC")]
    [InlineData(
        "https://open.spotify.com/track/4uLU6hMCjMI75M1A2tKUQC?si=abc",
        "4uLU6hMCjMI75M1A2tKUQC"
    )]
    [InlineData("https://open.spotify.com/album/abc", null)]
    [InlineData("https://example.org/track/abc", null)]
    [InlineData("просто текст", null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void TrackIdIsExtractedFromKnownForms(string input, string? expected)
    {
        Assert.Equal(expected, _client.ExtractTrackId(input));
    }

    [Fact]
    public async Task SearchMapsFirstTrackFromResponse()
    {
        var trackJson = TrackJson("трек", "artist", 180_000);
        _handler.Respond($@"{{""tracks"":{{""items"":[{trackJson}]}}}}");

        var track = await _client.SearchTrackAsync(
            " запрос ",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(track);
        Assert.Equal("трек", track!.TrackName);
        Assert.Equal("spotify:abc123", track.VideoId);
        Assert.Equal(["artist"], track.Authors!);
        Assert.Equal(TimeSpan.FromSeconds(180), track.Duration);
        Assert.Contains("q=запрос", Uri.UnescapeDataString(_handler.LastUrl));
        Assert.Contains("market=RU", _handler.LastUrl);
        Assert.Contains("type=track", _handler.LastUrl);
    }

    [Fact]
    public async Task SearchUsesDefaultMarketWhenNotConfigured()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"spotify-market-{Guid.NewGuid():N}")
            .Options;
        var factory = new TestDbContextFactory(options);
        using (var database = factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
            Seed(database, withDeviceId: false);
        }

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
        var auth = new SpotifyAuthService(
            factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );
        var client = new SpotifyApiClient(
            new HttpClient(_handler, disposeHandler: false),
            auth,
            Options.Create(new SpotifySoundRequestConfiguration { Enabled = true, Market = "  " }),
            NullLogger<SpotifyApiClient>.Instance
        );
        _handler.Respond(TrackJson("трек", "artist", 1_000));

        await client.SearchTrackAsync("запрос", TestContext.Current.CancellationToken);

        Assert.Contains("market=RU", _handler.LastUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankSearchQuerySkipsRequest(string query)
    {
        Assert.Null(await _client.SearchTrackAsync(query, TestContext.Current.CancellationToken));
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task FailedSearchYieldsNoTrack()
    {
        _handler.Respond("{}", HttpStatusCode.Forbidden);

        Assert.Null(
            await _client.SearchTrackAsync("запрос", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task SearchWithoutItemsYieldsNoTrack()
    {
        _handler.Respond("""{"tracks":{"items":[]}}""");

        Assert.Null(
            await _client.SearchTrackAsync("запрос", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task TrackIsResolvedBySpotifyUrl()
    {
        _handler.Respond(TrackJson("трек", "artist", 200_000));

        var track = await _client.ResolveTrackAsync(
            "https://open.spotify.com/track/abc123",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(track);
        Assert.Equal("трек", track!.TrackName);
        Assert.Contains("/tracks/abc123", _handler.LastUrl);
        Assert.Equal("Bearer " + AccessToken, _handler.LastAuthorization);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://example.org/abc")]
    public async Task UnknownTrackReferenceSkipsRequest(string queryOrUrl)
    {
        Assert.Null(
            await _client.ResolveTrackAsync(queryOrUrl, TestContext.Current.CancellationToken)
        );
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task LargestArtworkIsPicked()
    {
        _handler.Respond(
            """
            {"id":"abc123","name":"трек","duration_ms":1000,
             "album":{"images":[{"url":"https://i.example/small.png","width":64},
                                {"url":"https://i.example/big.png","width":640}]},
             "artists":[{"name":"первый"},{"name":"  "},{"name":"второй"}]}
            """
        );

        var track = await _client.ResolveTrackAsync(
            "https://open.spotify.com/track/abc123",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("https://i.example/big.png", track!.ArtworkUrl?.ToString());
        Assert.Equal(["первый", "второй"], track.Authors!);
    }

    [Fact]
    public async Task TrackWithoutNameGetsFallback()
    {
        _handler.Respond("""{"id":"abc123","duration_ms":1000}""");

        var track = await _client.ResolveTrackAsync(
            "https://open.spotify.com/track/abc123",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Unknown track", track!.TrackName);
        Assert.Null(track.ArtworkUrl);
    }

    [Fact]
    public async Task PlaySendsTrackUri()
    {
        _handler.Respond("{}");

        var played = await _client.PlayTrackAsync("abc123", TestContext.Current.CancellationToken);

        Assert.True(played);
        Assert.Equal(HttpMethod.Put, _handler.LastMethod);
        Assert.Contains("spotify:track:abc123", _handler.LastBody);
        Assert.Contains("/me/player/play", _handler.LastUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PlayWithoutTrackIdIsSkipped(string trackId)
    {
        Assert.False(await _client.PlayTrackAsync(trackId, TestContext.Current.CancellationToken));
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task RejectedPlayIsReported()
    {
        _handler.Respond("{}", HttpStatusCode.Forbidden);

        Assert.False(await _client.PlayTrackAsync("abc123", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PauseAndResumeUseTheirEndpoints()
    {
        _handler.Respond("{}");

        Assert.True(await _client.PauseAsync(TestContext.Current.CancellationToken));
        Assert.Contains("/me/player/pause", _handler.LastUrl);

        Assert.True(await _client.ResumeAsync(TestContext.Current.CancellationToken));
        Assert.Contains("/me/player/play", _handler.LastUrl);
    }

    [Fact]
    public async Task SkipUsesPostToNext()
    {
        _handler.Respond("{}");

        Assert.True(await _client.SkipToNextAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpMethod.Post, _handler.LastMethod);
        Assert.Contains("/me/player/next", _handler.LastUrl);
    }

    [Theory]
    [InlineData(-5, "0")]
    [InlineData(0, "0")]
    [InlineData(50, "50")]
    [InlineData(500, "100")]
    public async Task VolumeIsClampedToSpotifyRange(int volume, string expected)
    {
        _handler.Respond("{}");

        await _client.SetVolumeAsync(volume, TestContext.Current.CancellationToken);

        Assert.Contains($"volume_percent={expected}", _handler.LastUrl);
    }

    [Fact]
    public async Task PlaybackStateIsParsedFromResponse()
    {
        _handler.Respond(
            """{"is_playing":true,"progress_ms":1234,"item":{"id":"abc123","duration_ms":2000}}"""
        );

        var snapshot = await _client.GetCurrentPlaybackAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.True(snapshot!.IsPlaying);
        Assert.Equal("abc123", snapshot.TrackId);
        Assert.Equal(1234, snapshot.ProgressMs);
        Assert.Equal(2000, snapshot.DurationMs);
    }

    /// <summary>
    /// Ответ без тела (204) — это «ничего не играет», а не ошибка: иначе монитор
    /// считал бы остановку обрывом Spotify и перезапускал бы плеер.
    /// </summary>
    [Fact]
    public async Task EmptyPlaybackResponseMeansNothingPlays()
    {
        _handler.Respond("", HttpStatusCode.NoContent);

        var snapshot = await _client.GetCurrentPlaybackAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.False(snapshot!.IsPlaying);
        Assert.Null(snapshot.TrackId);
    }

    [Fact]
    public async Task FailedPlaybackRequestYieldsNoSnapshot()
    {
        _handler.Respond("{}", HttpStatusCode.Unauthorized);

        Assert.Null(await _client.GetCurrentPlaybackAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeviceFromApiIsAddedToPlayerUrlAndRemembered()
    {
        _handler.RespondByPath(
            "/me/player/devices",
            """{"devices":[{"id":"устройство-1","is_active":true}]}"""
        );
        _handler.Respond("{}");

        await _client.PauseAsync(TestContext.Current.CancellationToken);

        Assert.Contains("device_id=", Uri.UnescapeDataString(_handler.LastUrl));
        Assert.Contains("устройство-1", Uri.UnescapeDataString(_handler.LastUrl));
    }

    [Fact]
    public async Task ActiveDeviceWinsOverFirstInList()
    {
        _handler.RespondByPath(
            "/me/player/devices",
            """{"devices":[{"id":"первое","is_active":false},{"id":"активное","is_active":true}]}"""
        );
        _handler.Respond("{}");

        await _client.PauseAsync(TestContext.Current.CancellationToken);

        Assert.Contains("активное", Uri.UnescapeDataString(_handler.LastUrl));
    }

    /// <summary>
    /// Устройство спрашивается один раз и запоминается: список устройств Spotify
    /// меняется редко, а опрос на каждый трек съедал бы лимит запросов.
    /// </summary>
    [Fact]
    public async Task ResolvedDeviceIsCachedBetweenCalls()
    {
        _handler.RespondByPath(
            "/me/player/devices",
            """{"devices":[{"id":"устройство-1","is_active":true}]}"""
        );
        _handler.Respond("{}");

        await _client.PauseAsync(TestContext.Current.CancellationToken);
        await _client.SkipToNextAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, _handler.CountOf("/me/player/devices"));
    }

    [Fact]
    public async Task ConfiguredDeviceIsUsedWhenApiHasNoDevices()
    {
        _handler.RespondByPath("/me/player/devices", """{"devices":[]}""");
        _handler.Respond("{}");
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"spotify-device-{Guid.NewGuid():N}")
            .Options;
        var factory = new TestDbContextFactory(options);
        using (var database = factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
            Seed(database, withDeviceId: true);
        }

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
        var auth = new SpotifyAuthService(
            factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );
        var client = new SpotifyApiClient(
            new HttpClient(_handler, disposeHandler: false),
            auth,
            Options.Create(new SpotifySoundRequestConfiguration { Enabled = true }),
            NullLogger<SpotifyApiClient>.Instance
        );

        await client.PauseAsync(TestContext.Current.CancellationToken);

        Assert.Contains("saved-device", Uri.UnescapeDataString(_handler.LastUrl));
    }

    /// <summary>
    /// Без токена Spotify не должно выглядеть как «трек не найден»: вызовы
    /// возвращают «нет», а не ошибку 401 на каждый запрос плеера.
    /// </summary>
    [Fact]
    public async Task WithoutTokenNothingIsRequested()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"spotify-notoken-{Guid.NewGuid():N}")
            .Options;
        var factory = new TestDbContextFactory(options);
        using (var database = factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
            database.RootState.Add(
                new RootState
                {
                    Name = RootStateKeys.SoundRequestSpotifyClientId,
                    Value = string.Empty,
                }
            );
        }

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
        var auth = new SpotifyAuthService(
            factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );
        var client = new SpotifyApiClient(
            new HttpClient(_handler, disposeHandler: false),
            auth,
            Options.Create(new SpotifySoundRequestConfiguration { Enabled = true }),
            NullLogger<SpotifyApiClient>.Instance
        );

        Assert.False(await client.PauseAsync(TestContext.Current.CancellationToken));
        Assert.False(await client.PlayTrackAsync("abc", TestContext.Current.CancellationToken));
        Assert.Null(await client.GetCurrentPlaybackAsync(TestContext.Current.CancellationToken));
        Assert.Null(await client.SearchTrackAsync("запрос", TestContext.Current.CancellationToken));
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task NetworkFailureIsTurnedIntoFalse()
    {
        _handler.Throw(new HttpRequestException("нет сети"));

        Assert.False(await _client.SkipToNextAsync(TestContext.Current.CancellationToken));
        Assert.Null(await _client.GetCurrentPlaybackAsync(TestContext.Current.CancellationToken));
    }

    private static string TrackJson(string name, string artist, int durationMs) =>
        JsonSerializer.Serialize(
            new
            {
                id = "abc123",
                name,
                duration_ms = durationMs,
                album = new
                {
                    images = new[] { new { url = "https://i.example/big.png", width = 640 } },
                },
                artists = new[] { new { name = artist } },
            }
        );

    private static void Seed(MediaDbContext database, bool withDeviceId)
    {
        database.RootState.Add(
            new RootState { Name = RootStateKeys.SoundRequestSpotifyClientId, Value = "client" }
        );
        database.RootState.Add(
            new RootState { Name = RootStateKeys.SoundRequestSpotifyClientSecret, Value = "secret" }
        );
        database.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.SoundRequestSpotifyRefreshToken,
                Value = "refresh",
            }
        );
        database.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.SoundRequestSpotifyAccessToken,
                Value = AccessToken,
            }
        );
        database.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.SoundRequestSpotifyAccessTokenExpiresAtUtc,
                Value = DateTime.UtcNow.AddHours(1).ToString("O"),
            }
        );

        if (withDeviceId)
        {
            database.RootState.Add(
                new RootState
                {
                    Name = RootStateKeys.SoundRequestSpotifyDeviceId,
                    Value = "saved-device",
                }
            );
        }

        database.SaveChanges();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _byPath = new(StringComparer.Ordinal);
        private string _body = "{}";

        public List<Recorded> Requests { get; } = [];

        public string LastUrl => Requests[^1].Url;

        public HttpMethod? LastMethod => Requests[^1].Method;

        public string LastBody => Requests[^1].Body;

        public string LastAuthorization => Requests[^1].Authorization;

        public void Respond(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _body = body;
            DefaultStatus = status;
        }

        public HttpStatusCode DefaultStatus { get; private set; } = HttpStatusCode.OK;

        public void RespondByPath(string path, string body) => _byPath[path] = body;

        public int CountOf(string path) =>
            Requests.Count(request => request.Url.Contains(path, StringComparison.Ordinal));

        public void Throw(Exception exception) => Failure = exception;

        public Exception? Failure { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(
                new Recorded(
                    url,
                    request.Method,
                    body,
                    request.Headers.Authorization?.ToString() ?? string.Empty
                )
            );

            if (Failure is not null)
            {
                throw Failure;
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var mappedPath = _byPath
                .FirstOrDefault(entry => path.EndsWith(entry.Key, StringComparison.Ordinal))
                .Value;
            var payload = mappedPath ?? _body;

            return new HttpResponseMessage(DefaultStatus)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
                RequestMessage = request,
            };
        }
    }

    private sealed record Recorded(
        string Url,
        HttpMethod Method,
        string Body,
        string Authorization
    );
}
