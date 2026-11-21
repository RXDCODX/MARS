using MARS.Shared.Contracts.Messaging;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/ITwitchChatClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/ITwitchChatClient.cs

/// <summary>
/// Отправка сообщения в чат Twitch через MARS.Twitch.Api (REST, internal).
/// </summary>
public interface ITwitchChatClient
{
    Task SendAsync(TwitchChatSendRequest request, CancellationToken cancellationToken = default);
}
