using MARS.Shared.Messaging;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Chat;
using TwitchLib.Client.Interfaces;
using Wolverine;

namespace MARS.TwitchCore.Services.Chat;

/// <summary>
/// Слушатель <c>mars.chat.send</c>: единственная точка отправки сообщений в чат
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
public sealed class ChatSendListener(ITwitchClient client)
{
    private const int MaxMessageLength = 500;

    public async Task Handle(ChatSendEvent message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Message))
        {
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
