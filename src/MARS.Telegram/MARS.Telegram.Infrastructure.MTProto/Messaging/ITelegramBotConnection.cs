using TL;

namespace MARS.Telegram.Infrastructure.MTProto.Messaging;

/// <summary>
/// Поверхность клиента MTProto, которой пользуются обработчики и отправитель.
/// </summary>
/// <remarks>
/// <para>
/// За интерфейсом — намеренно только то, что сервис вызывает на самом деле.
/// Методы <c>WTelegram.Client</c> невиртуальные, поэтому подменить клиент иначе
/// нельзя, а юнит-тест, ушедший в сеть, проверял бы сеть, а не код.
/// </para>
/// <para>
/// Имена параметров повторяют оригинальные: иначе вызовы перестали бы собираться.
/// </para>
/// </remarks>
public interface ITelegramBotConnection : IDisposable
{
    /// <summary>
    /// Скачивает файл сообщения.
    /// </summary>
    Task DownloadFileAsync(
        Document document,
        Stream output,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Скачивает фото в указанном размере. Размер выбирает вызывающий: у Telegram
    /// их несколько, и первый — уменьшенная копия.
    /// </summary>
    Task DownloadFileAsync(
        Photo photo,
        Stream output,
        PhotoSize size,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Отправляет текст. Разметка передаётся готовыми сущностями: у Bot API её
    /// разбирал Telegram по <c>ParseMode</c>, у MTProto разбираем мы.
    /// </summary>
    Task SendTextAsync(
        InputPeer peer,
        string text,
        MessageEntity[]? entities,
        CancellationToken cancellationToken = default
    );

    /// <summary>Пересылает сообщение в другой чат от имени бота.</summary>
    Task ForwardAsync(
        InputPeer from,
        int messageId,
        InputPeer to,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Загружает файл для последующей отправки как медиа.
    /// </summary>
    Task<InputFileBase> UploadFileAsync(Stream stream, string fileName);

    /// <summary>
    /// Отправляет медиа с подписью.
    /// </summary>
    Task SendMediaAsync(
        InputPeer peer,
        InputMedia media,
        string caption,
        MessageEntity[]? entities,
        long replyToMessageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Разрешает канал/супергруппу по её идентификатору в <see cref="InputPeerChannel"/>
    /// с настоящим <c>access_hash</c>. Ботам Telegram разрешает нулевой хеш, но
    /// разрешение здесь даёт рабочий хеш (и сразу показывает, доступен ли канал:
    /// <c>CHANNEL_PRIVATE</c>/<c>CHANNEL_INVALID</c>), а не неясный
    /// <c>PEER_ID_INVALID</c> на отправке.
    /// </summary>
    Task<InputPeerChannel?> ResolveChannelAsync(
        long channelId,
        CancellationToken cancellationToken = default
    );
}
