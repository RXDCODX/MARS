using MARS.TwitchCore.Configuration;
using Microsoft.Extensions.Configuration;

namespace MARS.TwitchCore.Tests.Configuration;

/// <summary>
/// Адресаты уведомлений о переавторизации Twitch.
/// </summary>
/// <remarks>
/// Список приходит из файла стенда как <c>Telegram__AdminIds__0</c>, то есть
/// числовым индексом в имени ключа. Опечатка в ключе дала бы пустой список без
/// ошибки: уведомление просто ушло бы в никуда, и владелец не узнал бы, что
/// сервис просит токен.
/// </remarks>
public class TelegramConfigurationTests
{
    [Fact]
    public void Адресаты_читаются_из_секции_Telegram()
    {
        var options = new TelegramConfiguration();

        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "токен-бота",
                    ["Telegram:AdminIds:0"] = "402763435",
                    ["Telegram:AdminIds:1"] = "5252",
                }
            )
            .Build()
            .GetSection(TelegramConfiguration.TelegramSection)
            .Bind(options);

        Assert.Equal([402763435L, 5252L], options.AdminIds);
    }
}
