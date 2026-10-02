using TL;

namespace MARS.Telegram.Services.PrivateChannelsResender;

/// <summary>
/// Поверхность клиента WTelegram, которой пользуется пересылка каналов.
///
/// За интерфейсом — намеренно только то, что сервис вызывает на самом деле.
/// <c>WTelegram.Client</c> умеет авторизацию по номеру телефона и держит сессию
/// на диске, поэтому его нельзя ни собрать в тесте, ни подменить: конструктор
/// уходит в сеть. Имена параметров повторяют оригинальные, иначе именованные
/// аргументы в сервисе перестали бы компилироваться.
/// </summary>
public interface IWTelegramChannelClient : IDisposable
{
    /// <summary>
    /// Обновления приходят пачками, и обработчик должен быть асинхронным: одно
    /// событие может содержать несколько новых сообщений, а пересылка каждого
    /// занимает секунды.
    /// </summary>
    event Func<UpdatesBase, Task> OnUpdates;

    Task<Messages_Chats> Messages_GetAllChats();

    Task<Messages_MessagesBase?> Messages_GetHistory(
        InputPeer peer,
        int offset_id,
        int add_offset,
        int limit
    );

    Task Messages_SendMedia(InputPeer peer, InputMedia media, string message, long random_id);

    Task<bool> Messages_MarkDialogUnread(InputDialogPeerBase peer, bool unread = false);

    Task Channels_DeleteMessages(InputChannelBase channel, int[] messageIds);

    Task SendAlbumAsync(InputPeer peer, ICollection<InputMedia> album, string caption);

    Task<InputFileBase> UploadFileAsync(Stream stream, string fileName);

    /// <summary>
    /// Авторизован ли клиент: без входа у клиента нет списка каналов.
    /// </summary>
    bool IsAuthorized { get; }

    Task<string> DownloadFileAsync(Document document, Stream output);

    Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size);
}
