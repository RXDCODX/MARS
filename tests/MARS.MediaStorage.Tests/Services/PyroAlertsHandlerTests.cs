using MARS.MediaStorage.Services.PyroAlerts;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Пересылка медиа из Telegram в хранилище.
///
/// Медиа приходит из личных сообщений боту и показывается в оверлее. Проверяется,
/// что без вложения в хранилище ничего не уходит: пустая плашка на экране никому не
/// нужна.
/// </summary>
public class PyroAlertsHandlerTests
{
    private readonly Mock<IAlertNotifier> _notifier = new();
    private readonly PyroAlertsHandler _handler = new(
        new PyroAlertsHelper(NullLogger<PyroAlertsHelper>.Instance),
        Mock.Of<IAlertNotifier>()
    );

    public PyroAlertsHandlerTests() =>
        _handler = new PyroAlertsHandler(
            new PyroAlertsHelper(NullLogger<PyroAlertsHelper>.Instance),
            _notifier.Object
        );

    /// <summary>
    /// Сообщение без вложения не сохраняется: хранить нечего, а очередь оповещений
    /// засорялась бы.
    /// </summary>
    [Fact]
    public async Task TextWithoutFileIsNotStored()
    {
        await _handler.HandAlert(Mock.Of<ITelegramBotClient>(), Update());

        _notifier.Verify(notifier => notifier.SendAlertAsync(It.IsAny<MediaDto>()), Times.Never);
    }

    [Fact]
    public async Task NonMessageUpdateIsIgnored()
    {
        await _handler.HandAlert(
            Mock.Of<ITelegramBotClient>(),
            new Update
            {
                Id = 1,
                Message = new Message
                {
                    Id = 1,
                    Date = DateTime.Now,
                    Chat = new Chat { Id = 1, Type = ChatType.Private },
                },
            }
        );

        _notifier.Verify(notifier => notifier.SendAlertAsync(It.IsAny<MediaDto>()), Times.Never);
    }

    private static Update Update() =>
        new()
        {
            Id = 1,
            Message = new Message
            {
                Id = 1,
                Date = DateTime.Now,
                Text = "привет",
                Chat = new Chat
                {
                    Id = -100,
                    Type = ChatType.Channel,
                    Username = "mars",
                },
            },
        };
}
