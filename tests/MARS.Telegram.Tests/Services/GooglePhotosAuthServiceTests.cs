using System.Net;
using MARS.Shared.Models;
using MARS.Telegram.Configuration;
using MARS.Telegram.Entities;
using MARS.Telegram.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Авторизация Google Photos.
///
/// Токен хранится в состоянии чата и имеет срок. Проверяется, что просроченный
/// токен обновляется по сохранённому refresh-токену, а не выдаётся зрителю: с
/// просроченным токеном Google отвечает отказом, и загрузка фото молча не
/// работала бы.
/// </summary>
public class GooglePhotosAuthServiceTests
{
    private const string TokenJson = """
        {"access_token":"access-1","refresh_token":"refresh-1","expires_in":3600}
        """;

    private readonly ChatTestDbContextFactory _factory = new();
    private readonly StubHandler _handler = new();

    [Fact]
    public async Task AuthorizationUrlCarriesState()
    {
        var service = Create();

        var url = await service.GetAuthorizationUrlAsync(Token);

        Assert.Contains("state=", url);
        Assert.Contains("client_id=test-client", url);
        var state = url.Split("state=")[1].Split('&')[0];
        Assert.Equal(state, await ReadAsync(RootStateKeys.GooglePhotosOAuthState));
    }

    /// <summary>
    /// Код обменивается на токены, и они сохраняются: иначе после перезапуска
    /// сервис снова спросил бы авторизацию.
    /// </summary>
    [Fact]
    public async Task CodeIsExchangedAndTokensAreStored()
    {
        var service = Create();

        var tokens = await service.ExchangeCodeForTokenAsync("code-1", Token);

        Assert.NotNull(tokens);
        Assert.Equal("access-1", tokens!.AccessToken);
        Assert.Equal("access-1", await ReadAsync(RootStateKeys.GooglePhotosAccessToken));
        Assert.Equal("refresh-1", await ReadAsync(RootStateKeys.GooglePhotosRefreshToken));
    }

    /// <summary>
    /// Пустой код не отправляется в Google: обмен без кода вернул бы отказ.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyCodeIsNotExchanged(string? code)
    {
        var service = Create();

        Assert.Null(await service.ExchangeCodeForTokenAsync(code!, Token));
        Assert.Equal(0, _handler.Requests);
    }

    [Fact]
    public async Task RejectedCodeYieldsNoTokens()
    {
        var service = Create(status: HttpStatusCode.BadRequest);

        Assert.Null(await service.ExchangeCodeForTokenAsync("code-1", Token));
    }

    /// <summary>
    /// Живой токен берётся из состояния без обращения к Google.
    /// </summary>
    [Fact]
    public async Task FreshTokenIsUsedAsIs()
    {
        await WriteAsync(RootStateKeys.GooglePhotosAccessToken, "access-1");
        await WriteAsync(
            RootStateKeys.GooglePhotosAccessTokenExpiresAtUtc,
            DateTime.UtcNow.AddHours(1).ToString("O")
        );

        var service = Create();

        Assert.Equal("access-1", await service.GetValidAccessTokenAsync(Token));
        Assert.True(await service.IsAuthorizedAsync(Token));
        Assert.Equal(0, _handler.Requests);
    }

    /// <summary>
    /// Просроченный токен обновляется по сохранённому refresh-токену.
    /// </summary>
    [Fact]
    public async Task ExpiredTokenIsRefreshed()
    {
        await WriteAsync(RootStateKeys.GooglePhotosAccessToken, "access-old");
        await WriteAsync(
            RootStateKeys.GooglePhotosAccessTokenExpiresAtUtc,
            DateTime.UtcNow.AddHours(-1).ToString("O")
        );
        await WriteAsync(RootStateKeys.GooglePhotosRefreshToken, "refresh-1");

        var service = Create();

        Assert.Equal("access-1", await service.GetValidAccessTokenAsync(Token));
    }

    /// <summary>
    /// Обновлённый токен сохраняется, иначе каждый запрос обновлял бы его заново.
    /// </summary>
    [Fact]
    public async Task RefreshedTokenIsStored()
    {
        await WriteAsync(RootStateKeys.GooglePhotosAccessToken, "access-old");
        await WriteAsync(
            RootStateKeys.GooglePhotosAccessTokenExpiresAtUtc,
            DateTime.UtcNow.AddHours(-1).ToString("O")
        );
        await WriteAsync(RootStateKeys.GooglePhotosRefreshToken, "refresh-1");

        await Create().GetValidAccessTokenAsync(Token);

        Assert.Equal("access-1", await ReadAsync(RootStateKeys.GooglePhotosAccessToken));
    }

    /// <summary>
    /// Без refresh-токена обновление невозможно, и сервис остаётся неавторизованным.
    /// </summary>
    [Fact]
    public async Task WithoutRefreshTokenServiceIsNotAuthorized()
    {
        await WriteAsync(RootStateKeys.GooglePhotosAccessToken, "access-old");
        await WriteAsync(
            RootStateKeys.GooglePhotosAccessTokenExpiresAtUtc,
            DateTime.UtcNow.AddHours(-1).ToString("O")
        );

        var service = Create();

        Assert.Null(await service.GetValidAccessTokenAsync(Token));
        Assert.False(await service.IsAuthorizedAsync(Token));
        Assert.Equal(0, _handler.Requests);
    }

    /// <summary>
    /// Отказ Google при обновлении не выдаёт протухший токен: иначе вызовы шли бы
    /// с заведомо неверными данными.
    /// </summary>
    [Fact]
    public async Task FailedRefreshLeavesServiceUnauthorized()
    {
        await WriteAsync(RootStateKeys.GooglePhotosAccessToken, "access-old");
        await WriteAsync(
            RootStateKeys.GooglePhotosAccessTokenExpiresAtUtc,
            DateTime.UtcNow.AddHours(-1).ToString("O")
        );
        await WriteAsync(RootStateKeys.GooglePhotosRefreshToken, "refresh-1");

        var service = Create(status: HttpStatusCode.BadRequest);

        Assert.Null(await service.GetValidAccessTokenAsync(Token));
    }

    /// <summary>
    /// Отсутствие авторизации не считается ошибкой: сервис запускается и без Google
    /// Photos.
    /// </summary>
    [Fact]
    public async Task FreshServiceIsNotAuthorized()
    {
        Assert.False(await Create().IsAuthorizedAsync(Token));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private GooglePhotosAuthService Create(HttpStatusCode status = HttpStatusCode.OK)
    {
        _handler.Status = status;

        return new GooglePhotosAuthService(
            _factory,
            Options.Create(
                new GooglePhotosConfiguration
                {
                    ClientId = "test-client",
                    ClientSecret = "test-secret",
                    RedirectUri = "https://example.test/callback",
                }
            ),
            new SingleHandlerFactory(_handler),
            NullLogger<GooglePhotosAuthService>.Instance
        );
    }

    private async Task WriteAsync(string key, string value)
    {
        await using var db = await _factory.CreateDbContextAsync(Token);
        var state = await db.RootState.FindAsync([key], Token);
        if (state is null)
        {
            db.RootState.Add(new RootState { Name = key, Value = value });
        }
        else
        {
            state.Value = value;
        }

        await db.SaveChangesAsync(Token);
    }

    private async Task<string> ReadAsync(string key)
    {
        await using var db = await _factory.CreateDbContextAsync(Token);
        var state = await db.RootState.FindAsync([key], Token);

        return state?.Value ?? string.Empty;
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;

            return Task.FromResult(
                new HttpResponseMessage(Status) { Content = new StringContent(TokenJson) }
            );
        }
    }
}
