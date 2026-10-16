using MARS.TwitchCore.Configuration;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace MARS.TwitchCore.Services.Management;

/// <summary>
/// Адрес, по которому Twitch возвращает код авторизации.
/// </summary>
/// <remarks>
/// <para>
/// Значение одно на оба места: ссылка авторизации и обмен кода на токен. Twitch
/// сравнивает <c>redirect_uri</c> посимвольно и отвергает обмен при расхождении, а
/// расхождение жило в коде: в ссылке был путь <c>/TwitchAuth/TwitchUserAuth</c>
/// (такого маршрута в сервисе нет), в обмене — <c>/api/twitchauth/user-auth</c>.
/// </para>
/// <para>
/// Приоритет у настройки: внутри контейнера адрес самого сервиса браузеру
/// недоступен, снаружи сервис публикуется через Gateway. Не настроен — берётся
/// адрес сервиса, чего хватает для запуска на хосте.
/// </para>
/// </remarks>
public sealed class TwitchAuthRedirectUriProvider(
    IOptions<TwitchConfiguration> options,
    IServer server
)
{
    /// <summary>
    /// Путь обработчика кода авторизации. Совпадение с маршрутом контроллера
    /// проверяется тестом: иначе ссылка снова уведёт туда, где обработчика нет.
    /// </summary>
    public const string CallbackPath = "/api/TwitchAuth/user-auth";

    /// <summary>
    /// Адрес обработчика кода: настройка, иначе адрес сервиса с
    /// <see cref="CallbackPath"/>. <c>null</c> — подставлять нечего.
    /// </summary>
    public string? Value => Compose(options.Value.AuthRedirectUri, ServerAddress());

    private string? ServerAddress() =>
        server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

    /// <summary>
    /// Правило сложения адреса — чистое, чтобы проверялось без сервера.
    /// </summary>
    /// <param name="configured">Адрес из конфигурации, может быть пустым.</param>
    /// <param name="serverAddress">Первый адрес сервиса, может быть пустым.</param>
    public static string? Compose(string? configured, string? serverAddress)
    {
        string? result = null;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            // Хвостовой слеш убран: иначе настроенный адрес и адрес в ссылке
            // разошлись бы на символ, а Twitch сравнивает строки точно.
            result = configured.Trim().TrimEnd('/');
        }
        else if (!string.IsNullOrWhiteSpace(serverAddress))
        {
            result = serverAddress.TrimEnd('/') + CallbackPath;
        }

        return result;
    }
}
