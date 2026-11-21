using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Отправка сообщения в чат Twitch через MARS.Twitch.Api (REST, internal).
/// </summary>
public interface ITwitchChatClient
{
    Task SendAsync(TwitchChatSendRequest request, CancellationToken cancellationToken = default);
}
