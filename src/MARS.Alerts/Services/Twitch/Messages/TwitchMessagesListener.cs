using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Messages;

/// <summary>
/// Независимый слушатель Twitch-сообщений в собственной очереди MARS.Alerts.
/// Получает обычные сообщения, ввод через reward и уведомления об удалении.
/// Не запускает алерты и не изменяет существующие обработчики.
/// </summary>
public class TwitchMessagesListener(
    IOptions<RabbitMqOptions> options,
    ILogger<TwitchMessagesListener> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        RabbitMqConfig.AlertsTwitchMessagesQueue,
        [
            RabbitMqConfig.MessageReceived,
            RabbitMqConfig.RewardInputMessage,
            RabbitMqConfig.MessageDeleted,
        ],
        logger
    )
{
    protected override Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        switch (routingKey)
        {
            case RabbitMqConfig.MessageReceived:
            case RabbitMqConfig.RewardInputMessage:
            {
                var message = Deserialize<ChatEventMessageEvent>(json);

                if (message is null)
                {
                    throw new InvalidOperationException(
                        $"Twitch chat message on '{routingKey}' could not be deserialized."
                    );
                }

                return OnMessageReceivedAsync(routingKey, message, ct);
            }

            case RabbitMqConfig.MessageDeleted:
            {
                var message = Deserialize<TwitchMessageDeletedEvent>(json);

                if (message is null)
                {
                    throw new InvalidOperationException(
                        $"Twitch message deletion on '{routingKey}' could not be deserialized."
                    );
                }

                return OnMessageDeletedAsync(message, ct);
            }

            default:
                logger.LogWarning("Unhandled Twitch message routing key {RoutingKey}", routingKey);
                return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Точка расширения для обработки обычного сообщения или текста reward-input.
    /// <paramref name="routingKey"/> позволяет различать эти два потока.
    /// </summary>
    protected virtual Task OnMessageReceivedAsync(
        string routingKey,
        ChatEventMessageEvent message,
        CancellationToken ct
    )
    {
        logger.LogDebug(
            "Twitch chat message received from {UserName} ({RoutingKey})",
            message.UserName,
            routingKey
        );

        return Task.CompletedTask;
    }

    /// <summary>Точка расширения для обработки удаления сообщения Twitch.</summary>
    protected virtual Task OnMessageDeletedAsync(
        TwitchMessageDeletedEvent message,
        CancellationToken ct
    )
    {
        logger.LogDebug(
            "Twitch chat message {MessageId} deleted in {Channel}",
            message.MessageId,
            message.Channel
        );

        return Task.CompletedTask;
    }
}
