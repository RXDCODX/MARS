using MARS.Telegram.Infrastructure.Integrations.Telegram.Messaging;
using MARS.Telegram.Infrastructure.Integrations.Telegram.Models;

namespace MARS.Telegram.Infrastructure.Integrations.Telegram.BotService;

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
