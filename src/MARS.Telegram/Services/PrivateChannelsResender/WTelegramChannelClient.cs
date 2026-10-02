using TL;

namespace MARS.Telegram.Services.PrivateChannelsResender;

/// <summary>
/// Тонкая обёртка над <see cref="WTelegramClient"/>: переадресует вызовы
/// <see cref="IWTelegramChannelClient"/> настоящему клиенту.
///
/// Обёртка нужна, чтобы production-код продолжал работать с настоящим клиентом, а
/// тесты подставляли заглушку с тем же интерфейсом. Осмысленной логики здесь нет:
/// все проверки живут в самом сервисе. Необязательные параметры клиента
/// (смещения, даты, разметка) намеренно не вынесены в интерфейс: сервис их не
/// задаёт.
/// </summary>
public sealed class WTelegramChannelClient(WTelegramClient client) : IWTelegramChannelClient
{
    public event Func<UpdatesBase, Task> OnUpdates
    {
        add => client.OnUpdates += value;
        remove => client.OnUpdates -= value;
    }

    public Task<Messages_Chats> Messages_GetAllChats() => client.Messages_GetAllChats();

    public Task<Messages_MessagesBase?> Messages_GetHistory(
        InputPeer peer,
        int offset_id,
        int add_offset,
        int limit
    ) => client.Messages_GetHistory(peer, offset_id, add_offset: add_offset, limit: limit);

    public async Task Messages_SendMedia(
        InputPeer peer,
        InputMedia media,
        string message,
        long random_id
    ) => await client.Messages_SendMedia(peer, media, message, random_id: random_id);

    public Task<bool> Messages_MarkDialogUnread(InputDialogPeerBase peer, bool unread = false) =>
        client.Messages_MarkDialogUnread(peer, unread: unread);

    public async Task Channels_DeleteMessages(InputChannelBase channel, int[] messageIds) =>
        await client.Channels_DeleteMessages(channel, messageIds);

    public Task SendAlbumAsync(InputPeer peer, ICollection<InputMedia> album, string caption) =>
        client.SendAlbumAsync(peer, album, caption);

    public async Task<InputFileBase> UploadFileAsync(Stream stream, string fileName) =>
        await client.UploadFileAsync(stream, fileName);

    /// <summary>
    /// Авторизован ли клиент: без входа у него нет списка каналов.
    /// </summary>
    public bool IsAuthorized => client.User != null;

    public Task<string> DownloadFileAsync(Document document, Stream output) =>
        client.DownloadFileAsync(document, output);

    /// <summary>
    /// Тип файла приходит перечислением, а сервис использует его как часть имени
    /// файла, поэтому наружу отдаётся строка — иначе в имя файла попало бы
    /// «photo/Jpeg».
    /// </summary>
    public async Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size) =>
        (await client.DownloadFileAsync(photo, output, size)).ToString();

    public void Dispose() => client.Dispose();
}
