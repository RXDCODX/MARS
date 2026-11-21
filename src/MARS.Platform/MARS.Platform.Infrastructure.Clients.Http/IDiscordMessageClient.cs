using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Отправка сообщения в канал Discord через MARS.Discord (REST, internal).
/// </summary>
public interface IDiscordMessageClient
{
    Task SendAsync(
        DiscordMessageSendRequest request,
        CancellationToken cancellationToken = default
    );
}
