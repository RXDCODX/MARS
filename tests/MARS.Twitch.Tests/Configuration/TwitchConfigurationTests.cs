using MARS.Twitch.Application.Configuration;
using Microsoft.Extensions.Configuration;

namespace MARS.Twitch.Tests.Configuration;

/// <summary>
/// Секция конфигурации Twitch.
/// </summary>
/// <remarks>
/// Секция обязана называться <c>Twitch</c>: с таким именем её задают переменные
/// окружения файла стенда и читает <c>Program.cs</c> (<c>Twitch:ClientId</c>).
/// Токены в конфигурации больше не хранятся: они получаются только через
/// OAuth-флоу самого сервиса (кнопка «Авторизовать» на /twitch-auth или ссылка
/// из Telegram), поэтому из секции читаются только ClientId/Secret и адрес
/// обработчика.
/// </remarks>
public class TwitchConfigurationTests
{
    private static TwitchConfiguration Bind(params (string Key, string? Value)[] settings) =>
        Bind(
            new Dictionary<string, string?>(
                settings.Select(setting => new KeyValuePair<string, string?>(
                    setting.Key,
                    setting.Value
                ))
            )
        );

    private static TwitchConfiguration Bind(IDictionary<string, string?> settings)
    {
        var options = new TwitchConfiguration();

        new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build()
            .GetSection(TwitchConfiguration.SectionName)
            .Bind(options);

        return options;
    }

    [Fact]
    public void Секция_та_же_что_у_переменных_окружения()
    {
        Assert.Equal("Twitch", TwitchConfiguration.SectionName);
    }

    /// <summary>
    /// Адрес обработчика читается из той же секции: именно он попадает в
    /// <c>Twitch__AuthRedirectUri</c> файла стенда.
    /// </summary>
    [Fact]
    public void Адрес_обработчика_читается_из_секции()
    {
        var options = Bind(
            ("Twitch:AuthRedirectUri", "https://stand.example/api/TwitchAuth/user-auth")
        );

        Assert.Equal("https://stand.example/api/TwitchAuth/user-auth", options.AuthRedirectUri);
    }
}
