using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Отправка/редактирование сообщений в Telegram через MARS.Telegram (REST, internal).
/// </summary>
public interface ITelegramMessageClient
{
    Task SendAsync(
        TelegramMessageSendRequest request,
        CancellationToken cancellationToken = default
    );

    Task EditInlineAsync(
        TelegramInlineEditRequest request,
        CancellationToken cancellationToken = default
    );
}
