using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Options;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Services.Chat;

/// <summary>
/// Потребитель <c>twitch.chat.send</c>: единственная точка отправки сообщений в чат
/// для остальных микросервисов.
/// </summary>
/// <remarks>
/// MARS.WaifuGacha раньше поднимал собственное IRC/EventSub-подключение теми же
/// учётными данными, что и MARS.TwitchCore. Twitch закрывает более старое из двух
/// соединений, поэтому один из сервисов молча переставал получать события
/// (блокер №19 из аудита PR). Теперь WaifuGacha публикует
/// <see cref="ChatSendEvent"/>, а сообщение уходит через единственное соединение
/// этого сервиса.
/// </remarks>
public class TwitchChatSendConsumer(
    IOptions<RabbitMqOptions> options,
    ITwitchClient client,
    ILogger<TwitchChatSendConsumer> logger
) : RabbitMqConsumerBase(options.Value, "twitchcore", QueueName, [RabbitMqConfig.ChatSend], logger)
{
    public const string QueueName = "twitchcore.chatsend";

    private const int MaxMessageLength = 500;

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var chatSend = Deserialize<ChatSendEvent>(json);

        if (chatSend is null || string.IsNullOrWhiteSpace(chatSend.Message))
        {
            logger.LogWarning("Chat send on {RoutingKey} has no message, skipped", routingKey);
            return;
        }

        var channel = string.IsNullOrWhiteSpace(chatSend.Channel)
            ? TwitchConstants.Channel
            : chatSend.Channel.Trim();

        var normalized = chatSend
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
