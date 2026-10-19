using MARS.Telegram.Models;
using MARS.Telegram.Services.Messaging;

namespace MARS.Telegram.Services.BotService;

/// <summary>
/// Обработчик входящего сообщения.
/// </summary>
/// <remarks>
/// Контракт перестал быть Bot API-овским: раньше обработчик получал
/// <c>Update</c> и клиента Bot API. Теперь сообщение приходит нормализованным, а
/// вместо клиента — узкое подключение: файлы качаются им же, и подменять его
/// можно заглушкой.
/// </remarks>
public interface ITelegramIncomingHandler
{
    Task HandleAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message,
        CancellationToken cancellationToken
    );
}
