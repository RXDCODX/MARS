using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Api.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Interfaces;
using Wolverine;

namespace MARS.Twitch.Api.Services.Chat;

/// <summary>
/// Потребитель <see cref="ChatSendEvent">запросов на отправку сообщения в чат</see>:
/// единственная точка отправки сообщений в чат для остальных микросервисов.
/// </summary>
/// <remarks>
/// MARS.WaifuGacha раньше поднимал собственное IRC/EventSub-подключение теми же
/// учётными данными, что и MARS.Twitch.Api. Twitch закрывает более старое из двух
/// соединений, поэтому один из сервисов молча переставал получать события
/// (блокер №19 из аудита PR). Теперь WaifuGacha публикует
/// <see cref="ChatSendEvent"/>, а сообщение уходит через единственное соединение
/// этого сервиса.
/// </remarks>
public sealed class TwitchChatSendListener(
    ITwitchClient client,
    ILogger<TwitchChatSendListener> logger
) : MarsEventListener<ChatSendEvent>
{
    private const int MaxMessageLength = 500;

    public override async Task Handle(ChatSendEvent message, CancellationToken ct)
    {
        if (message is null || string.IsNullOrWhiteSpace(message.Message))
        {
            logger.LogWarning("Chat send has no message, skipped");
            return;
        }

        var channel = string.IsNullOrWhiteSpace(message.Channel)
            ? TwitchConstants.Channel
            : message.Channel.Trim();

        var normalized = message
            .Message.Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("  ", " ")
            .Trim();

        while (normalized.Length > MaxMessageLength)
        {
            var splitAt = normalized.LastIndexOf(' ', MaxMessageLength);

            if (splitAt <= 0)
            {
                splitAt = MaxMessageLength;
            }

            await client.SendMessageAsync(channel, normalized[..splitAt]);
            normalized = normalized[splitAt..].TrimStart(' ');
        }

        if (normalized.Length > 0)
        {
            await client.SendMessageAsync(channel, normalized);
        }
    }
}
