using MARS.Shared.Contracts.Messaging;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/ITelegramMessageClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/ITelegramMessageClient.cs

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
