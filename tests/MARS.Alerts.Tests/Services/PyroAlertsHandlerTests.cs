using MARS.Alerts.Application.Twitch.Rewards.Services.PyroAlerts;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Пересылка медиа из Telegram в оверлей.
///
/// Обработчик сам ничего не рассылает: он только раскладывает вложения по
/// хранилищу в памяти. Поэтому проверяется, что сообщение без вложения ничего
/// не оставляет в хранилище, а обновление, не являющееся сообщением, не
/// обрабатывается вовсе.
/// </summary>
[Collection("MemoryStorage")]
public class PyroAlertsHandlerTests : IDisposable
{
    private readonly PyroAlertsHandler _handler = new(
        new PyroAlertsHelper(NullLogger<PyroAlertsHelper>.Instance)
    );

    public PyroAlertsHandlerTests() => MemoryStorage.ClearStorage();

    public void Dispose() => MemoryStorage.ClearStorage();

    /// <summary>
    /// Текст без вложения в хранилище не попадает: хранить нечего, а оверлею
    /// нечего показывать.
    /// </summary>
    [Fact]
    public async Task TextWithoutFileLeavesStorageEmpty()
    {
        await _handler.HandAlert(
            Mock.Of<ITelegramBotClient>(),
            Update(text: "привет")
        );

        Assert.Equal(0, MemoryStorage.FileCount);
    }

    /// <summary>
    /// Обновление, не являющееся сообщением, игнорируется: обрабатывать нечего.
    /// </summary>
    [Fact]
    public async Task NonMessageUpdateIsIgnored()
    {
        await _handler.HandAlert(
            Mock.Of<ITelegramBotClient>(),
            new Update { Id = 1 }
        );

        Assert.Equal(0, MemoryStorage.FileCount);
    }

    private static Update Update(string? text = null) =>
        new()
        {
            Id = 1,
            Message = new Message
            {
                Id = 1,
                Date = DateTime.Now,
                Text = text,
                Chat = new Chat
                {
                    Id = -100,
                    Type = ChatType.Channel,
                    Username = "mars",
                },
            },
        };
}
