using TL;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Telegram.Infrastructure.MTProto.Messaging;

/// <summary>
/// Обёртка над <see cref="WTelegramClient"/>: ровно те вызовы, что нужны сервису.
/// </summary>
/// <remarks>
/// Обёртка нужна не для удобства, а для проверяемости: методы клиента
/// невиртуальные, и без неё обработчики пришлось бы проверять живым Telegram.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class WTelegramBotConnection(WTelegramClient client) : ITelegramBotConnection
{
    /// <inheritdoc />
    public async Task DownloadFileAsync(
        Document document,
        Stream output,
        CancellationToken cancellationToken = default
    ) => await client.DownloadFileAsync(document, output);

    /// <inheritdoc />
    public async Task DownloadFileAsync(
        Photo photo,
        Stream output,
        PhotoSize size,
        CancellationToken cancellationToken = default
    ) => await client.DownloadFileAsync(photo, output, size);

    /// <inheritdoc />
    public async Task SendTextAsync(
        InputPeer peer,
        string text,
        MessageEntity[]? entities,
        CancellationToken cancellationToken = default
    ) =>
        await client.SendMessageAsync(
            peer,
            text,
            media: null,
            reply_to_msg_id: 0,
            entities,
            schedule_date: default,
            preview: global::WTelegram.Client.LinkPreview.Disabled
        );

    /// <inheritdoc />
    public async Task<InputFileBase> UploadFileAsync(Stream stream, string fileName) =>
        await client.UploadFileAsync(stream, fileName);

    /// <inheritdoc />
    public async Task SendMediaAsync(
        InputPeer peer,
        InputMedia media,
        string caption,
        MessageEntity[]? entities,
        long replyToMessageId,
        CancellationToken cancellationToken = default
    ) =>
        await client.SendMessageAsync(
            peer,
            caption ?? string.Empty,
            media,
            reply_to_msg_id: (int)replyToMessageId,
            entities,
            schedule_date: default,
            preview: global::WTelegram.Client.LinkPreview.Disabled
        );

    /// <inheritdoc />
    public async Task ForwardAsync(
        InputPeer from,
        int messageId,
        InputPeer to,
        CancellationToken cancellationToken = default
    ) => await client.ForwardMessagesAsync(from, [messageId], to);

    /// <inheritdoc />
    public async Task<InputPeerChannel?> ResolveChannelAsync(
        long channelId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Ботам разрешён нулевой хеш: Channels_GetChannels с нулевым хешем
            // возвращает реальный access_hash канала, если бот в нём участвует.
            var input = new InputChannel(channelId, 0);
            var result = await client.Channels_GetChannels(input);
            if (result.chats.TryGetValue(channelId, out var chat) && chat is Channel channel)
            {
                return new InputPeerChannel(channel.id, channel.access_hash);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
