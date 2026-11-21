using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Interfaces;
using Wolverine;

namespace MARS.Twitch.Infrastructure.Services.Chat;

/// <summary>
/// Потребитель <see cref="ChatAnnouncementEvent"/>: отправляет сообщение в чат
/// Twitch через единственное IRC-подключение (объявление как обычное сообщение,
/// так как Helix SendChatAnnouncementAsync недоступен в этой версии TwitchLib).
/// </summary>
public sealed class TwitchChatAnnouncementListener(
    ITwitchClient client,
    ILogger<TwitchChatAnnouncementListener> logger
) : MarsEventListener<ChatAnnouncementEvent>
{
    public override async Task Handle(ChatAnnouncementEvent message, CancellationToken ct)
    {
        if (message is null || string.IsNullOrWhiteSpace(message.Message))
        {
            logger.LogWarning("Chat announcement has no message, skipped");
            return;
        }

        var channel = string.IsNullOrWhiteSpace(message.Channel)
            ? TwitchConstants.Channel
            : message.Channel.Trim();

        try
        {
            await client.SendMessageAsync(channel, message.Message);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }
}
