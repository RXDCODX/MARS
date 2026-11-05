using MARS.Shared.Messaging;
using MARS.WaifuGacha.Services;
using Wolverine;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Слушатель сообщений чата: сверяет накопленные уведомления с текущим временем
/// и шлёт в чат те, чей кулдаун истёк.
/// </summary>
public sealed class ChatCooldownListener(RollCooldownNotificationService service)
{
    public Task Handle(ChatMessageReceivedEvent message, CancellationToken cancellationToken) =>
        service.OnChatMessageAsync(message, cancellationToken);
}
