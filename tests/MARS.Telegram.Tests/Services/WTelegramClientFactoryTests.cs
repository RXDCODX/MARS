using System.Text;
using MARS.Telegram.Configuration;
using MARS.Telegram.Services.WTelegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Сборка клиента WTelegram: адрес прокси, конфигурация приложения и сессия.
/// </summary>
/// <remarks>
/// Сам вход в аккаунт проверяется живым запуском: он уходит в сеть, и юнит-тест
/// такого не проверяет честно. Здесь — всё, что решается без сети: какой адрес
/// у прокси, какие ключи уходят клиенту и что секрет берётся из того же файла
/// окружения, что и у контейнера прокси.
/// </remarks>
public class WTelegramClientFactoryTests
{
    [Fact]
    public void Адрес_прокси_Собирается_Из_Сервера_Порта_И_Секрета()
    {
        var factory = Factory(
            new TelegramConfiguration
            {
                ProxyServer = "tg-ws-proxy",
                ProxyPort = 1443,
                ProxySecret = "dd0123456789abcdef0123456789abcd",
            }
        );

        Assert.Equal(
            "tg://proxy?server=tg-ws-proxy&port=1443&secret=dd0123456789abcdef0123456789abcd",
            factory.ResolveProxyUrl()
        );
    }

    /// <summary>
    /// Без секрета ссылка всё равно строится: прокси может быть открытым, и
    /// молчаливый <c>null</c> означал бы «идём напрямую» вместо «идём в прокси».
    /// </summary>
    [Fact]
    public void Без_секрета_Ссылка_Остаётся_Но_Без_Параметра()
    {
        var factory = Factory(
            new TelegramConfiguration { ProxyServer = "localhost", ProxyPort = 1443 }
        );

        Assert.Equal("tg://proxy?server=localhost&port=1443", factory.ResolveProxyUrl());
    }

    /// <summary>
    /// Прокси не задан — клиент идёт в Telegram напрямую, и ссылка не выдумывается.
    /// </summary>
    [Fact]
    public void Без_Прокси_Ссылка_Не_Строится()
    {
        var factory = Factory(new TelegramConfiguration());

        Assert.Null(factory.ResolveProxyUrl());
    }

    /// <summary>
    /// Секрет прокси лежит в том же файле окружения, что и переменная контейнера
    /// прокси. Своя настройка пустая — берётся переменная: дублировать секрет в
    /// двух местах нельзя, расходятсясь молча, и прокси перестаёт пускать.
    /// </summary>
    [Fact]
    public void Секрет_Берётся_Из_Переменной_Окружения_Прокси()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["TG_WS_PROXY_SECRET"] = "dd-secret-из-окружения",
                }
            )
            .Build();

        var factory = Factory(
            new TelegramConfiguration { ProxyServer = "tg-ws-proxy", ProxyPort = 1443 },
            configuration: configuration
        );

        Assert.Contains("secret=dd-secret-из-окружения", factory.ResolveProxyUrl());
    }

    /// <summary>
    /// Своя настройка главнее переменной окружения: иначе правка compose тихо
    /// перебила бы значение, заданное явно.
    /// </summary>
    [Fact]
    public void Своя_Настройка_Секрета_Главнее_Переменной_Окружения()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["TG_WS_PROXY_SECRET"] = "dd-из-окружения" }
            )
            .Build();

        var factory = Factory(
            new TelegramConfiguration
            {
                ProxyServer = "tg-ws-proxy",
                ProxyPort = 1443,
                ProxySecret = "dd-из-настройки",
            },
            configuration: configuration
        );

        Assert.Contains("secret=dd-из-настройки", factory.ResolveProxyUrl());
    }

    /// <summary>
    /// Клиент собирается с сессией из указанного потока и уходит в прокси: без
    /// адреса он соединялся бы напрямую, а Bot API с машины закрыт.
    /// </summary>
    [Fact]
    public void Клиент_Получает_Прокси_И_Сессию()
    {
        var factory = Factory(
            new TelegramConfiguration
            {
                ApiId = "1234567",
                ApiHash = "0123456789abcdef0123456789abcdef",
                ProxyServer = "localhost",
                ProxyPort = 1443,
            }
        );

        using var session = new MemoryStream();

        using var client = factory.Create(session);

        Assert.NotNull(client);
        Assert.Contains("server=localhost", client.MTProxyUrl);
    }

    /// <summary>
    /// Клиент собирается с пустой сессией: авторизация в неё допишет своё
    /// состояние, и требовать непустой поток значило бы запретить первый вход.
    /// </summary>
    [Fact]
    public void Клиент_Собирается_С_Пустой_Сессией()
    {
        var factory = Factory(
            new TelegramConfiguration
            {
                ApiId = "1234567",
                ApiHash = "0123456789abcdef0123456789abcdef",
            }
        );

        using var session = new MemoryStream();
        using var client = factory.Create(session);

        Assert.NotNull(client);
    }

    /// <summary>
    /// Без учётных данных приложения клиент не создаётся — и падает он внутри
    /// библиотеки сообщением про «config value», от которого до настройки далеко.
    /// Ошибка обязана называть ключ, который надо задать.
    /// </summary>
    [Fact]
    public void Без_Учётных_Данных_Ошибка_Называет_Ключ()
    {
        var factory = Factory(new TelegramConfiguration());

        var error = Assert.Throws<InvalidOperationException>(() =>
            factory.Create(new MemoryStream())
        );

        Assert.Contains("Telegram__ApiId", error.Message);
        Assert.Contains("Telegram__ApiHash", error.Message);
    }

    /// <summary>
    /// Ключи уходят клиенту под теми именами, что он знает. Опечатка здесь стоит
    /// дорого: клиент не жалуется на лишний ключ, а требует тот, которого не
    /// передали, — «You must provide a config value for verification_code».
    /// Живой запуск на этой машине именно так и остановился.
    /// </summary>
    [Fact]
    public void Ключи_Конфигурации_Названы_Как_Их_Ждёт_Клиент()
    {
        var factory = Factory(
            new TelegramConfiguration
            {
                ApiId = "1234567",
                ApiHash = "0123456789abcdef0123456789abcdef",
                PhoneNumber = "+79991234567",
                Password = "двухфакторный",
                LoginCode = "54321",
            }
        );

        var config = factory.BuildConfig();

        Assert.Equal("1234567", config["api_id"]);
        Assert.Equal("0123456789abcdef0123456789abcdef", config["api_hash"]);
        Assert.Equal("+79991234567", config["phone_number"]);
        Assert.Equal("двухфакторный", config["password"]);
        Assert.Equal("54321", config["verification_code"]);
        Assert.Equal("verification_code", WTelegramClientFactory.VerificationCodeKey);
    }

    /// <summary>
    /// Пустой код подтверждения ключом не заменяется: клиент сам спросит его при
    /// первом входе, а передавать пустую строну означало бы «код введён, он пуст».
    /// </summary>
    [Fact]
    public void Без_Кода_Ключ_Не_Добавляется()
    {
        var factory = Factory(
            new TelegramConfiguration { ApiId = "1234567", ApiHash = "0123456789abcdef" }
        );

        Assert.DoesNotContain("verification_code", factory.BuildConfig().Keys);
    }

    private static WTelegramClientFactory Factory(
        TelegramConfiguration telegramOptions,
        IConfiguration? configuration = null
    )
    {
        var source = configuration ?? new ConfigurationBuilder().AddInMemoryCollection().Build();

        return new WTelegramClientFactory(
            Options.Create(telegramOptions),
            source,
            new ChatTestDbContextFactory(),
            NullLogger<WTelegramClientFactory>.Instance
        );
    }
}
