using System.Text.Json;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Messages;

/// <summary>
/// Ретранслирует обычное сообщение чата Twitch в оверлейный хаб.
/// </summary>
/// <remarks>
/// <para>
/// В монолите сообщение уходило в оверлей напрямую (<c>notifier.NewMessage</c>),
/// потому что хаб жил в том же процессе. В микросервисах хаб живёт в
/// <c>MARS.Alerts</c>, а <c>MARS.TwitchCore</c> публикует <see cref="ChatEventMessageEvent"/>
/// в RabbitMQ — этот листенер его подхватывает и кладёт в broadcaster, откуда
/// <c>HubEventRelay</c> рассылает его в браузер как <c>NewMessage</c>.
/// </para>
/// <para>
/// Наградные сообщения (<c>CustomRewardId</c>) сюда не приходят: продюсер
/// (<c>TwitchMessagesPublisher</c>) отделяет их в <c>RewardInputChatMessageEvent</c>,
/// как и в монолите.
/// </para>
/// </remarks>
public sealed class OverlayChatMessageListener(
    ILogger<OverlayChatMessageListener> logger,
    EventBroadcaster<TelegramusEvent> broadcaster
) : MarsEventListener<ChatEventMessageEvent>
{
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

    public override async Task Handle(ChatEventMessageEvent message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogDebug("Chat message received from {UserName}", message.UserName);

        // Сериализация в camelCase (Web-настройки): фронтенд разбирает
        // MessageJson через normalizeChatMessage, который читает поля userName,
        // chatColor, isModerator, isVip, message. Другой casing сломал бы плашку.
        var messageJson = JsonSerializer.Serialize(
            message,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );

        await _broadcaster.BroadcastAsync(
            new TelegramusEvent
            {
                NewMessage = new NewMessageEvent { Id = message.Id, MessageJson = messageJson },
            }
        );
    }
}
