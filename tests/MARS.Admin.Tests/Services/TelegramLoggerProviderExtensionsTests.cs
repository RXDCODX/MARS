using MARS.Admin.CustomLoggers.TelegramLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Регистрация Telegram-логгера в общей инфраструктуре логирования. Провайдер
/// не собирается: проверяется, что он попал в коллекцию сервисов, а отправка
/// разбирается в <c>TelegramLoggerTests</c> на заглушке клиента.
/// </summary>
public class TelegramLoggerProviderExtensionsTests
{
    [Fact]
    public void ProviderIsRegisteredForValidOptions()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
            logging.AddTelegramLogger(
                new TelegramLoggerOptions
                {
                    BotToken = "123456:AAHtest",
                    ChatId = [42L],
                    SourceName = "MARS",
                },
                null
            )
        );

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILoggerProvider));
        Assert.Contains(
            services,
            descriptor => descriptor.ImplementationInstance is TelegramLoggerProvider
        );
    }

    [Fact]
    public void ConfigureOverloadBuildsOptions()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
            logging.AddTelegramLogger(options =>
            {
                options.BotToken = "123456:AAHtest";
                options.ChatId = [1L, 2L];
                options.SourceName = "MARS";
            })
        );

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILoggerProvider));
    }

    /// <summary>
    /// Без токена или без адресата логирование в Telegram не подключается: иначе
    /// каждый вызов Log уходил бы в сеть с заведомо нерабочими настройками.
    /// </summary>
    [Theory]
    [InlineData("", "42")]
    [InlineData("   ", "42")]
    [InlineData("123456:AAHtest", "")]
    public void RegistrationIsSkippedForIncompleteOptions(string botToken, string chatId)
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
            logging.AddTelegramLogger(
                new TelegramLoggerOptions
                {
                    BotToken = botToken,
                    ChatId = string.IsNullOrWhiteSpace(chatId) ? [] : [long.Parse(chatId)],
                    SourceName = "MARS",
                }
            )
        );

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(ILoggerProvider)
        );
    }

    /// <summary>
    /// Невалидный токен роняет <c>TelegramBotClient</c>, и регистрация молча
    /// пропускается: старт сервиса важнее логов в Telegram.
    /// </summary>
    [Fact]
    public void InvalidTokenDoesNotRegisterProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
            logging.AddTelegramLogger(
                new TelegramLoggerOptions
                {
                    BotToken = "не токен",
                    ChatId = [42L],
                    SourceName = "MARS",
                }
            )
        );

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(ILoggerProvider)
        );
    }
}
