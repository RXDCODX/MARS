using MARS.Shared.Models;
using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Controllers;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Services.Management;
using MARS.TwitchCore.Tests.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api.Core;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Controllers;

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
    [Fact]
    public async Task Без_кода_возвращается_бизнес_ошибка()
    {
        var (result, action) = await CallAsync(code: null);

        Assert.IsType<OkObjectResult>(action);
        Assert.False(result.Success);
        Assert.Contains("код", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Пустой код — тот же случай: <c>?code=</c> приходит пустым значением.
    /// </summary>
    [Fact]
    public async Task Пустой_код_возвращается_бизнес_ошибка()
    {
        var (result, action) = await CallAsync(code: "");

        Assert.IsType<OkObjectResult>(action);
        Assert.False(result.Success);
    }

    /// <summary>
    /// Отказ пользователя приходит как <c>error=access_denied</c> и без кода.
    /// Назвать это «код не предоставлен» — значит отправить искать причину не туда.
    /// </summary>
    [Fact]
    public async Task Отказ_пользователя_называется_своим_именем()
    {
        var (result, action) = await CallAsync(code: null, error: "access_denied");

        Assert.IsType<OkObjectResult>(action);
        Assert.False(result.Success);
        Assert.Contains("access_denied", result.ErrorMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Адрес обработчика определить не удалось — это тоже настройка, о ней и
    /// говорим: без неё ссылка авторизации никуда не ведёт.
    /// </summary>
    [Fact]
    public async Task Без_адреса_обработчика_объясняем_что_задать()
    {
        var (result, action) = await CallAsync(code: "код", withAddress: false);

        Assert.IsType<OkObjectResult>(action);
        Assert.False(result.Success);
        Assert.Contains("Twitch__AuthRedirectUri", result.ErrorMessage!, StringComparison.Ordinal);
    }

    private static async Task<(OperationResult<object?> Result, IActionResult Action)> CallAsync(
        string? code,
        string? error = null,
        bool withAddress = true
    )
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

        var controller = new TwitchAuthController(
            TwitchTokenServiceStub.Create(new TwitchTestDbContextFactory()),
            ApiStub(),
            new TwitchAuthRedirectUriProvider(
                Options.Create(new TwitchConfiguration { AuthRedirectUri = string.Empty }),
                server.Object
            ),
            NullLogger<TwitchAuthController>.Instance
        );

        var actionResult = await controller.TwitchUserAuth(code, error);
        var ok = (OkObjectResult)actionResult.Result!;

        return ((OperationResult<object?>)ok.Value!, actionResult.Result!);
    }

    private static ITwitchAPI ApiStub()
    {
        var api = new Mock<ITwitchAPI>();
        api.SetupGet(instance => instance.Settings).Returns(new ApiSettings { ClientId = "id" });

        return api.Object;
    }
}
