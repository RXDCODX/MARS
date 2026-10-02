using MARS.Alerts.Services.PyroAlerts;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Пересылка медиа из Telegram в оверлей.
///
/// Проверяется, что в оверлей уходит только то, что действительно является медиа.
/// Сообщение без файла не должно превращаться в пустую плашку: зритель увидел бы
/// заглушку вместо оповещения.
/// </summary>
public class PyroAlertsHandlerTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly PyroAlertsHandler _handler = new(
        new PyroAlertsHelper(NullLogger<PyroAlertsHelper>.Instance),
        Mock.Of<ITelegramusNotifier>()
    );

    public PyroAlertsHandlerTests() =>
        _handler = new PyroAlertsHandler(
            new PyroAlertsHelper(NullLogger<PyroAlertsHelper>.Instance),
            _notifier.Object
        );

    /// <summary>
    /// Текст без вложения в оверлей не попадает: показывать нечего.
    /// </summary>
    [Fact]
    public async Task TextWithoutFileIsNotForwarded()
    {
        await _handler.HandAlert(
            Mock.Of<ITelegramBotClient>(),
            Update(MessageType.Text, text: "привет")
        );

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    /// <summary>
    /// Сообщение не от чата игнорируется: оно пришло не из того чата, за которым
    /// следит бот.
    /// </summary>
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

        _notifier.Verify(instance => instance.Alert(It.IsAny<MediaDto>()), Times.Never);
    }

    private static Update Update(MessageType type, string? text = null) =>
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
