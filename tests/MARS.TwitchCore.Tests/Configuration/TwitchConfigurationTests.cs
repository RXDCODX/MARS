using MARS.TwitchCore.Configuration;
using Microsoft.Extensions.Configuration;

namespace MARS.TwitchCore.Tests.Configuration;

/// <summary>
/// Секция конфигурации Twitch.
/// </summary>
/// <remarks>
/// Секция обязана называться <c>Twitch</c>: с таким именем её задают переменные
/// окружения файла стенда (<c>Twitch__OAuth</c>, <c>Twitch__AccessToken</c>) и
/// читает <c>Program.cs</c> (<c>Twitch:ClientId</c>). Имя <c>TwitchConfig</c>
/// осталось после переноса из монолита, и из-за него <c>TwitchConnectionManager</c>
/// получал пустой OAuth — подключение к чату пропускалось с предупреждением,
/// хотя переменная в окружении была задана.
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
    /// Все четыре значения читаются из одной секции: OAuth чата, токены для API и
    /// адрес обработчика. Именно они попадают в <c>Twitch__*</c> файла стенда.
    /// </summary>
    [Fact]
    public void Токены_и_адрес_обработчика_читаются_из_секции()
    {
        var options = Bind(
            ("Twitch:OAuth", "oauth-токен"),
            ("Twitch:AccessToken", "access-токен"),
            ("Twitch:RefreshToken", "refresh-токен"),
            ("Twitch:AccessTokenExpiresIn", "02:00:00"),
            ("Twitch:AuthRedirectUri", "https://stand.example/api/TwitchAuth/user-auth")
        );

        Assert.Equal("oauth-токен", options.OAuth);
        Assert.Equal("access-токен", options.AccessToken);
        Assert.Equal("refresh-токен", options.RefreshToken);
        Assert.Equal(TimeSpan.FromHours(2), options.AccessTokenExpiresIn);
        Assert.Equal("https://stand.example/api/TwitchAuth/user-auth", options.AuthRedirectUri);
    }

    /// <summary>
    /// Без секции срок остаётся неопределённым, а не «нулевым»: нулевой срок
    /// читался бы как «токен истёк» и заставил сервис обновлять токен при каждой
    /// проверке.
    /// </summary>
    [Fact]
    public void Без_секции_срок_не_задан()
    {
        var options = Bind();

        Assert.Null(options.AccessTokenExpiresIn);
    }
}
