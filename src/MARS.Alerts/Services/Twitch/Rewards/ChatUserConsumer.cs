using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Потребитель <c>chat.received</c>: единственный источник данных для
/// <see cref="IChatUserTrackingHandler"/>.
/// Раньше поток сообщений чата не читался ни одним сервисом вообще, поэтому
/// MIKU MIKU BEAM всегда показывал пустой набор участников.
/// </summary>
public class ChatUserConsumer(
    IOptions<RabbitMqOptions> options,
    IEnumerable<IChatUserTrackingHandler> handlers,
    ILogger<ChatUserConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        RabbitMqConfig.Queues.AlertsChatUsers,
        [RabbitMqConfig.RoutingKeys.ChatReceived],
        logger
    )
{
    private readonly IReadOnlyList<IChatUserTrackingHandler> _handlers = [.. handlers];

    protected override Task HandleMessageAsync(string routingKey, string json, CancellationToken ct)
    {
        if (_handlers.Count == 0)
        {
            return Task.CompletedTask;
        }

        var chatMessage = Deserialize<ChatMessageEvent>(json);

        if (chatMessage is null || string.IsNullOrWhiteSpace(chatMessage.UserId))
        {
            logger.LogWarning(
                "Chat message on {RoutingKey} has no deserializable user, skipped",
                routingKey
            );
            return Task.CompletedTask;
        }

        foreach (var handler in _handlers)
        {
            handler.TrackChatUser(chatMessage);
        }

        return Task.CompletedTask;
    }
}
