using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests;

/// <summary>
/// Настройка Telegram, одинаковая для всех сервисов.
///
/// Токен берётся из конфигурации или окружения, а список администраторов
/// собирается вручную: compose подставляет пустую строку для незаданного
/// <c>TELEGRAM_ADMIN_ID</c>, и приведение "" к long падало бы, роняя хост при
/// старте.
/// </summary>
public class TelegramConfigurationExtensionsTests
{
    [Fact]
    public void BotTokenIsReadFromConfiguration()
    {
        var configuration = Configuration(("Telegram:BotToken", "токен-из-конфига"));

        Assert.Equal("токен-из-конфига", configuration.ResolveTelegramBotToken());
    }

    /// <summary>
    /// Старое имя секции поддерживается: конфигурация монолита осталась в части
    /// сервисов.
    /// </summary>
    [Fact]
    public void LegacyTokenNameIsSupported()
    {
        var configuration = Configuration(("Telegram:Token", "старый-токен"));

        Assert.Equal("старый-токен", configuration.ResolveTelegramBotToken());
    }

    /// <summary>
    /// Без токена возвращается null, а не пустая строка: сервис без Telegram должен
    /// запускаться, а не падать на проверке.
    /// </summary>
    [Fact]
    public void MissingTokenYieldsNull()
    {
        Assert.Null(Configuration().ResolveTelegramBotToken());
    }

    [Fact]
    public void AdminIdsAreReadFromSection()
    {
        var configuration = Configuration(
            ("Telegram:BotToken", "токен"),
            ("Telegram:AdminIds:0", "111"),
            ("Telegram:AdminIds:1", "222")
        );

        var options = TelegramOptions(configuration);

        Assert.Equal([111L, 222L], options.AdminIds ?? []);
        Assert.Equal("токен", options.BotToken);
    }

    /// <summary>
    /// Пустое значение администратора пропускается: compose оставляет его для
    /// незаданной переменной, и разбор не должен ронять хост.
    /// </summary>
    [Fact]
    public void BlankAdminIdIsSkipped()
    {
        var configuration = Configuration(
            ("Telegram:BotToken", "токен"),
            ("Telegram:AdminIds:0", ""),
            ("Telegram:AdminIds:1", "333")
        );

        Assert.Equal([333L], TelegramOptions(configuration).AdminIds ?? []);
    }

    [Fact]
    public void AdminlessConfigurationIsAllowed()
    {
        var configuration = Configuration(("Telegram:BotToken", "токен"));

        Assert.Empty(TelegramOptions(configuration).AdminIds ?? []);
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value))
            )
            .Build();

    private static TelegramConfig TelegramOptions(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddMarsTelegramOptions(configuration);

        return services.BuildServiceProvider().GetRequiredService<IOptions<TelegramConfig>>().Value;
    }
}
