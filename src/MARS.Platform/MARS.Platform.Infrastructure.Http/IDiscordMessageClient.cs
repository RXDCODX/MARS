using MARS.Shared.Contracts.Messaging;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/IDiscordMessageClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/IDiscordMessageClient.cs

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
