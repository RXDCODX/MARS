using MARS.TwitchCore.Controllers;
using MARS.TwitchCore.Services.Management;
using Microsoft.AspNetCore.Mvc;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Адрес, по которому Twitch возвращает код авторизации.
/// </summary>
/// <remarks>
/// Значение одно и то же идёт в ссылку авторизации и в обмен кода на токен:
/// Twitch отвергает обмен, если <c>redirect_uri</c> не совпал с тем, что был в
/// ссылке. Раньше это были разные строки — ссылка вела на
/// <c>/TwitchAuth/TwitchUserAuth</c> (маршрута такого в сервисе нет), а обмен
/// уходил с <c>/api/twitchauth/user-auth</c>, — и первый токен было негде получить.
/// </remarks>
public class TwitchAuthRedirectUriProviderTests
{
    [Fact]
    public void Настроенный_адрес_выигрывает_у_адреса_сервиса()
    {
        var uri = TwitchAuthRedirectUriProvider.Compose(
            "https://stand.example/api/TwitchAuth/user-auth",
            "http://localhost:6002"
        );

        Assert.Equal("https://stand.example/api/TwitchAuth/user-auth", uri);
    }

    /// <summary>
    /// Хвостовой слеш убирается: иначе настроенный адрес и адрес в ссылке разошлись
    /// бы на символ, а Twitch сравнивает строки точно.
    /// </summary>
    [Fact]
    public void Хвостовой_слеш_убирается()
    {
        var uri = TwitchAuthRedirectUriProvider.Compose(
            "https://stand.example/api/TwitchAuth/user-auth/",
            null
        );

        Assert.Equal("https://stand.example/api/TwitchAuth/user-auth", uri);
    }

    [Fact]
    public void Без_настройки_берётся_адрес_сервиса()
    {
        var uri = TwitchAuthRedirectUriProvider.Compose(null, "http://localhost:6002");

        Assert.Equal("http://localhost:6002" + TwitchAuthRedirectUriProvider.CallbackPath, uri);
    }

    /// <summary>
    /// Ни адреса сервиса, ни настройки — вернуть нечего: молча отправлять ссылку с
    /// относительным адресом значило бы отправить её в никуда.
    /// </summary>
    [Fact]
    public void Без_адреса_и_настройки_адреса_нет()
    {
        var uri = TwitchAuthRedirectUriProvider.Compose(null, null);

        Assert.Null(uri);
    }

    /// <summary>
    /// Путь в константе обязан совпадать с маршрутом контроллера: иначе ссылка
    /// авторизации снова уведёт туда, где обработчика нет, и расхождение не заметит
    /// ни один тест.
    /// </summary>
    [Fact]
    public void Путь_обработчика_совпадает_с_маршрутом_контроллера()
    {
        var route = typeof(TwitchAuthController)
            .GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>()
            .Single()
            .Template!;
        var action = typeof(TwitchAuthController)
            .GetMethod(nameof(TwitchAuthController.TwitchUserAuth))!
            .GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>()
            .Single()
            .Template!;
        var controller = typeof(TwitchAuthController).Name.Replace(
            "Controller",
            string.Empty,
            StringComparison.Ordinal
        );

        Assert.Equal(
            TwitchAuthRedirectUriProvider.CallbackPath,
            $"/{route}/{action}".Replace("[controller]", controller)
        );
    }
}
