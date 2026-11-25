using MARS.Shared.Contracts.Models;
using MARS.Shared.Kernel;
using MARS.Twitch.Api.Controllers;
using MARS.Twitch.Application.Configuration;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.Connection;
using MARS.Twitch.Infrastructure.Services.EventSub;
using MARS.Twitch.Infrastructure.Services.Management;
using MARS.Twitch.Tests.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api.Core;
using TwitchLib.Api.Interfaces;

namespace MARS.Twitch.Tests.Controllers;

/// <summary>
/// Приём кода авторизации Twitch.
/// </summary>
/// <remarks>
/// Отсутствие кода — бизнес-ошибка, а не ошибка запроса: с непустым
/// <c>string code</c> фреймворк отвечает 400 <c>ValidationProblemDetails</c> раньше
/// тела метода, и владелец видит «One or more validation errors occurred» вместо
/// причины. Плюс Twitch при отказе пользователя возвращает не код, а
/// <c>error=access_denied</c>: такой запрос тоже надо назвать своим именем.
/// </remarks>
public class TwitchAuthControllerTests
{
    private const string CallbackPath = "/twitch-auth/callback";

    [Fact]
    public async Task Успешный_обмен_редиректит_на_страницу_результата()
    {
        var (redirect, _) = await CallAsync(code: "код", withAddress: true, withExchange: true);

        Assert.NotNull(redirect);
        Assert.Equal(CallbackPath + "?status=success", redirect!.Url);
    }

    [Fact]
    public async Task Отказ_пользователя_редиректит_с_ошибкой()
    {
        var (redirect, _) = await CallAsync(code: null, error: "access_denied");

        Assert.NotNull(redirect);
        Assert.StartsWith(
            CallbackPath + "?status=error&message=",
            redirect!.Url,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "access_denied",
            Uri.UnescapeDataString(redirect.Url),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task Без_кода_редиректит_с_ошибкой()
    {
        var (redirect, _) = await CallAsync(code: null);

        Assert.NotNull(redirect);
        Assert.StartsWith(
            CallbackPath + "?status=error&message=",
            redirect!.Url,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "код",
            Uri.UnescapeDataString(redirect.Url),
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task Пустой_код_редиректит_с_ошибкой()
    {
        var (redirect, _) = await CallAsync(code: "");

        Assert.NotNull(redirect);
        Assert.StartsWith(
            CallbackPath + "?status=error&message=",
            redirect!.Url,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task Без_адреса_обработчика_редиректит_с_подсказкой()
    {
        var (redirect, _) = await CallAsync(code: "код", withAddress: false);

        Assert.NotNull(redirect);
        Assert.StartsWith(
            CallbackPath + "?status=error&message=",
            redirect!.Url,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Twitch__AuthRedirectUri",
            Uri.UnescapeDataString(redirect.Url),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task AuthorizeUrl_использует_общий_набор_прав()
    {
        var controller = CreateController(withAddress: true, withExchange: false);
        var action = controller.GetAuthorizeUrl();
        var ok = (OkObjectResult)action.Result!;
        var result = (OperationResult<string>)ok.Value!;

        Assert.True(result.Success);
        var expected = TwitchAuthorizationLink.Build(
            "id",
            "http://localhost:6002/api/TwitchAuth/user-auth"
        );
        Assert.Equal(expected, result.Result);
    }

    private static async Task<(RedirectResult? Redirect, IActionResult Action)> CallAsync(
        string? code,
        string? error = null,
        bool withAddress = true,
        bool withExchange = false
    )
    {
        var controller = CreateController(withAddress, withExchange);
        var actionResult = await controller.TwitchUserAuth(code, error);
        var redirect = actionResult.Result as RedirectResult;
        return (redirect, actionResult.Result!);
    }

    private static TwitchAuthController CreateController(bool withAddress, bool withExchange)
    {
        var features = new FeatureCollection();

        if (withAddress)
        {
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add("http://localhost:6002");
            features.Set<IServerAddressesFeature>(addresses);
        }

        var server = new Mock<IServer>();
        server.Setup(instance => instance.Features).Returns(features);

        TwitchAuthController controller = withExchange
            ? new TestableTwitchAuthController(
                TwitchTokenServiceStub.Create(new TwitchTestDbContextFactory()),
                ApiStub(),
                new TwitchAuthRedirectUriProvider(
                    Options.Create(new TwitchConfiguration { AuthRedirectUri = string.Empty }),
                    server.Object
                ),
                null,
                null,
                NullLogger<TwitchAuthController>.Instance
            )
            : new TwitchAuthController(
                TwitchTokenServiceStub.Create(new TwitchTestDbContextFactory()),
                ApiStub(),
                new TwitchAuthRedirectUriProvider(
                    Options.Create(new TwitchConfiguration { AuthRedirectUri = string.Empty }),
                    server.Object
                ),
                null,
                null,
                NullLogger<TwitchAuthController>.Instance
            );

        return controller;
    }

    private static ITwitchAPI ApiStub()
    {
        var api = new Mock<ITwitchAPI>();
        api.SetupGet(instance => instance.Settings)
            .Returns(new ApiSettings { ClientId = "id", Secret = "secret" });

        return api.Object;
    }

    private sealed class TestableTwitchAuthController : TwitchAuthController
    {
        public TestableTwitchAuthController(
            TokenService tokenService,
            ITwitchAPI api,
            TwitchAuthRedirectUriProvider redirectUri,
            ITwitchConnectionState? twitchConnectionState,
            EventSubService? eventSubService,
            ILogger<TwitchAuthController> logger
        )
            : base(tokenService, api, redirectUri, twitchConnectionState, eventSubService, logger)
        { }

        protected override Task<(
            string AccessToken,
            string RefreshToken,
            int ExpiresIn
        )> ExchangeCodeAsync(string code, string redirectUriValue) =>
            Task.FromResult<(string, string, int)>(("access", "refresh", 3600));
    }
}
