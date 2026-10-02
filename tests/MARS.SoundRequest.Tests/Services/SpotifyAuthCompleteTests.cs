using System.Net;
using System.Text;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Tests.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Завершение авторизации Spotify.
///
/// Авторизация идёт через браузер, и код возвращается с <c>state</c>. Проверяется,
/// что подменённый код не принимается: иначе чужой ссылкой можно было бы
/// подключить чужой аккаунт к боту.
/// </summary>
public class SpotifyAuthCompleteTests
{
    private const string State = "state-1";
    private const string RedirectUri = "https://mars.example.org/spotify";
    private const string TokenJson = """
        {"access_token":"access-1","refresh_token":"refresh-1","expires_in":3600}
        """;
    private const string ProfileJson = """
        {"id":"user-1","display_name":"Pyro","product":"premium","images":[{"url":"https://img.test/1.png"}]}
        """;

    private readonly DbContextOptions<MediaDbContext> _options =
        new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"spotify-auth-{Guid.NewGuid():N}")
            .Options;

    /// <summary>
    /// Успешный обмен кода сохраняет токен и профиль: после перезапуска сервиса
    /// подключение не должно теряться.
    /// </summary>
    [Fact]
    public async Task SuccessfulExchangeStoresTokensAndProfile()
    {
        await SeedAsync();
        var handler = new SequenceHandler(
            [HttpStatusCode.OK, HttpStatusCode.OK],
            [TokenJson, ProfileJson]
        );
        var service = Create(handler);

        var result = await service.CompleteAuthorizationAsync("code-1", State, RedirectUri, Token);

        Assert.True(result.Success);
        Assert.Equal("Pyro", result.DisplayName);
        Assert.Equal("access-1", await ReadAsync(RootStateKeys.SoundRequestSpotifyAccessToken));
        Assert.Equal("Pyro", await ReadAsync(RootStateKeys.SoundRequestSpotifyDisplayName));
    }

    /// <summary>
    /// Чужой state не принимается: без проверки можно было бы подсунуть ссылку со
    /// своим кодом.
    /// </summary>
    [Fact]
    public async Task ForeignStateIsRejected()
    {
        await SeedAsync();
        var handler = new SequenceHandler([HttpStatusCode.OK], [TokenJson]);
        var service = Create(handler);

        var result = await service.CompleteAuthorizationAsync(
            "code-1",
            "чужой",
            RedirectUri,
            Token
        );

        Assert.False(result.Success);
        Assert.Equal(0, handler.Requests);
    }

    [Theory]
    [InlineData(null, State)]
    [InlineData("", State)]
    [InlineData("code-1", null)]
    [InlineData("code-1", "")]
    public async Task IncompleteRequestIsRejected(string? code, string? state)
    {
        await SeedAsync();
        var handler = new SequenceHandler([HttpStatusCode.OK], [TokenJson]);
        var service = Create(handler);

        var result = await service.CompleteAuthorizationAsync(code!, state!, RedirectUri, Token);

        Assert.False(result.Success);
        Assert.Equal(0, handler.Requests);
    }

    /// <summary>
    /// Отказ Spotify сохраняется как отказ, а не как успех с пустым токеном: иначе
    /// очередь играла бы в пустоту.
    /// </summary>
    [Fact]
    public async Task SpotifyRejectionIsReported()
    {
        await SeedAsync();
        var handler = new SequenceHandler([HttpStatusCode.BadRequest], ["denied"]);
        var service = Create(handler);

        var result = await service.CompleteAuthorizationAsync("code-1", State, RedirectUri, Token);

        Assert.False(result.Success);
        Assert.Empty(await ReadAsync(RootStateKeys.SoundRequestSpotifyAccessToken));
    }

    /// <summary>
    /// Ответ без access token считается неудачей: токен всё равно не с чем работать.
    /// </summary>
    [Fact]
    public async Task EmptyTokenIsRejected()
    {
        await SeedAsync();
        var handler = new SequenceHandler([HttpStatusCode.OK], ["""{"expires_in":3600}"""]);
        var service = Create(handler);

        var result = await service.CompleteAuthorizationAsync("code-1", State, RedirectUri, Token);

        Assert.False(result.Success);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private SpotifyAuthService Create(SequenceHandler handler) =>
        new(
            new TestDbContextFactory(_options),
            new ConfigurationBuilder()
                .AddInMemoryCollection([
                    new KeyValuePair<string, string?>("Spotify:ClientId", "client-1"),
                ])
                .Build(),
            new SingleHandlerFactory(handler),
            NullLogger<SpotifyAuthService>.Instance
        );

    private async Task SeedAsync()
    {
        await using var db = new MediaDbContext(_options);
        db.RootState.Add(
            new RootState { Name = RootStateKeys.SoundRequestSpotifyOAuthState, Value = State }
        );
        db.RootState.Add(
            new RootState { Name = RootStateKeys.SoundRequestSpotifyClientId, Value = "client-1" }
        );
        db.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.SoundRequestSpotifyClientSecret,
                Value = "secret-1",
            }
        );
        await db.SaveChangesAsync(Token);
    }

    private async Task<string> ReadAsync(string name)
    {
        await using var db = new MediaDbContext(_options);
        var state = await db
            .RootState.AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Name == name, Token);

        return state?.Value ?? string.Empty;
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>
    /// Ответы выдаются по очереди: обмен кода — один запрос, профиль — второй.
    /// </summary>
    private sealed class SequenceHandler(HttpStatusCode[] statuses, string[] bodies)
        : HttpMessageHandler
    {
        private int _index;

        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            var index = Math.Min(_index++, statuses.Length - 1);

            return Task.FromResult(
                new HttpResponseMessage(statuses[index])
                {
                    Content = new StringContent(bodies[index], Encoding.UTF8, "application/json"),
                }
            );
        }
    }
}
