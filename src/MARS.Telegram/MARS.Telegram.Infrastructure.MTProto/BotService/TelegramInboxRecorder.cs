using MARS.Telegram.Application.Models;
using MARS.Telegram.Application.Services;
using MARS.Telegram.Infrastructure.MTProto.Messaging;
using MARS.Telegram.Infrastructure.MTProto.Models;

namespace MARS.Telegram.Infrastructure.MTProto.BotService;

/// <summary>
/// Записывает каждое входящее сообщение в общий стор консоли.
/// </summary>
/// <remarks>
/// Регистрируется и как синглтон, и как <see cref="ITelegramIncomingHandler"/> —
/// по образцу других обработчиков. Пишет в потокобезопасный стор, поэтому
/// безопасен при вызове из MTProto-потока обновлений.
/// </remarks>
public sealed class TelegramInboxRecorder(TelegramInboxStore store) : ITelegramIncomingHandler
{
    public Task HandleAsync(
        ITelegramBotConnection client,
        TelegramIncomingMessage message,
        CancellationToken cancellationToken
    )
    {
        store.Add(
            new TelegramInboxEntry(
                DateTime.UtcNow,
                message.ChatId,
                message.MessageId,
                message.Text,
                message.IsChannelPost,
                message.HasMedia
            )
        );

        return Task.CompletedTask;
    }
}
