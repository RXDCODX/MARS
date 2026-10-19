using MARS.Telegram.Configuration;
using Microsoft.Extensions.Configuration;

namespace MARS.Telegram.Tests.Configuration;

/// <summary>
/// Чтение секции <c>Telegram</c>.
/// </summary>
/// <remarks>
/// Имя свойства — это имя секции, из которой биндер читает значение. Прежнее
/// <c>AdminIdsArray</c> ждало <c>Telegram:AdminIdsArray</c>, а переменная окружения
/// даёт <c>Telegram:AdminIds:0</c>, и список администраторов приходил пустым —
/// сервис был «настроен», а сообщения слать было некому.
/// </remarks>
public class TelegramConfigurationTests
{
    [Fact]
    public void Список_Админов_Читается_Из_Секции_AdminIds()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Telegram:AdminIds:0"] = "402763435",
                    ["Telegram:AdminIds:1"] = "42",
                }
            )
            .Build();

        var options = new TelegramConfiguration();
        configuration.GetSection(TelegramConfiguration.TelegramSection).Bind(options);

        Assert.Equal([402763435L, 42L], options.AdminIds);
    }

    /// <summary>
    /// Без настройки список пуст, а не null: иначе каждое обращение требовало бы
    /// проверки на null, а пустой массив для цикла уже безопасен.
    /// </summary>
    [Fact]
    public void Без_Настройки_Список_Пуст()
    {
        Assert.Empty(new TelegramConfiguration().AdminIds);
    }
}
