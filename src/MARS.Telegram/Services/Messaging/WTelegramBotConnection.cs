using TL;

namespace MARS.Telegram.Services.Messaging;

/// <summary>
/// Обёртка над <see cref="WTelegramClient"/>: ровно те вызовы, что нужны сервису.
/// </summary>
/// <remarks>
/// Обёртка нужна не для удобства, а для проверяемости: методы клиента
/// невиртуальные, и без неё обработчики пришлось бы проверять живым Telegram.
/// </remarks>
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
    public async Task ForwardAsync(
        InputPeer from,
        int messageId,
        InputPeer to,
        CancellationToken cancellationToken = default
    ) => await client.ForwardMessagesAsync(from, [messageId], to);

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
