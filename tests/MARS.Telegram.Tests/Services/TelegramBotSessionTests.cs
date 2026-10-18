using MARS.Telegram.Models;
using MARS.Telegram.Services.BotService;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Разбор обновлений MTProto в нормализованное сообщение.
/// </summary>
/// <remarks>
/// Обмен идёт по MTProto, и типы приходят другие: у сообщения адресат лежит в
/// <c>peer_id</c>, а фото и документ — внутри медиа. Ошибка здесь тихая и
/// разрушительная: обработчик получил бы пустой чат или не увидел бы фото.
/// </remarks>
public class TelegramBotSessionTests
{
    /// <summary>
    /// Пользователь: чат и адресат строятся из одноимённых полей.
    /// </summary>
    [Fact]
    public void Сообщение_Пользователя_Несёт_Чат_И_Текст()
    {
        var normalized = TelegramBotSession.Normalize(
            Message(new PeerUser { user_id = 402763435 }, id: 7, text: "привет"),
            isChannelPost: false
        );

        Assert.NotNull(normalized);
        Assert.Equal(402763435L, normalized.ChatId);
        Assert.Equal(7, normalized.MessageId);
        Assert.Equal("привет", normalized.Text);
        Assert.Equal(402763435L, Assert.IsType<InputPeerUser>(normalized.Peer).user_id);
        Assert.False(normalized.IsChannelPost);
    }

    /// <summary>
    /// Пост в канале — другой тип адресата: <c>InputPeerUser</c> с идентификатором
    /// канала отправить сообщение не дал бы.
    /// </summary>
    [Fact]
    public void Пост_В_Канале_Несёт_Адресата_Канала()
    {
        var normalized = TelegramBotSession.Normalize(
            Message(new PeerChannel { channel_id = -1001234567890 }, id: 3, text: "новость"),
            isChannelPost: true
        );

        Assert.NotNull(normalized);
        Assert.True(normalized.IsChannelPost);
        Assert.Equal(-1001234567890L, Assert.IsType<InputPeerChannel>(normalized.Peer).channel_id);
    }

    /// <summary>
    /// Группа — третий тип адресата, и он тоже не совпадает с пользователем.
    /// </summary>
    [Fact]
    public void Группа_Получает_Свой_Тип_Адресата()
    {
        var normalized = TelegramBotSession.Normalize(
            Message(new PeerChat { chat_id = 100500 }, id: 1, text: "всем привет"),
            isChannelPost: false
        );

        Assert.NotNull(normalized);
        Assert.Equal(100500L, Assert.IsType<InputPeerChat>(normalized.Peer).chat_id);
    }

    /// <summary>
    /// Фото достаётся из медиа и сохраняется объектом: по нему клиент качает файл,
    /// и копия означала бы второе место, где надо вспомнить про file_reference.
    /// </summary>
    [Fact]
    public void Фото_Достаётся_Из_Медиа()
    {
        var photo = new Photo { id = 1, sizes = [new PhotoSize { type = "y", size = 10 }] };
        var message = Message(new PeerUser { user_id = 1 }, id: 1, text: string.Empty);
        message.media = new MessageMediaPhoto { photo = photo };

        var normalized = TelegramBotSession.Normalize(message, isChannelPost: false);

        Assert.NotNull(normalized);
        Assert.Same(photo, normalized.Photo);
        Assert.Null(normalized.Document);
        Assert.True(normalized.HasMedia);
    }

    /// <summary>
    /// Документ достаётся так же, и без фото он тоже считается медиа.
    /// </summary>
    [Fact]
    public void Документ_Достаётся_Из_Медиа()
    {
        var document = new Document { id = 5, mime_type = "image/png" };
        var message = Message(new PeerUser { user_id = 1 }, id: 1, text: string.Empty);
        message.media = new MessageMediaDocument { document = document };

        var normalized = TelegramBotSession.Normalize(message, isChannelPost: false);

        Assert.NotNull(normalized);
        Assert.Same(document, normalized.Document);
        Assert.Null(normalized.Photo);
        Assert.True(normalized.HasMedia);
    }

    /// <summary>
    /// Альбом помечается общим идентификатором: без него каждое фото уехало бы
    /// отдельной страницей, а пользователь получил бы столько ссылок, сколько
    /// отправил фотографий.
    /// </summary>
    [Fact]
    public void Альбом_Помечается_Общим_Идентификатором()
    {
        var grouped = Message(new PeerUser { user_id = 1 }, id: 1, text: string.Empty);
        grouped.grouped_id = 4242;

        var normalized = TelegramBotSession.Normalize(grouped, isChannelPost: false);

        Assert.NotNull(normalized);
        Assert.Equal(4242, normalized.MediaGroupId);
    }

    /// <summary>
    /// Одиночное фото альбомом не считается: у Telegram в этом поле ноль.
    /// </summary>
    [Fact]
    public void Одиночное_Фото_Не_Помечается_Альбомом()
    {
        var normalized = TelegramBotSession.Normalize(
            Message(new PeerUser { user_id = 1 }, id: 1, text: string.Empty),
            isChannelPost: false
        );

        Assert.NotNull(normalized);
        Assert.Null(normalized.MediaGroupId);
    }

    /// <summary>
    /// Сообщение без адресата нормализовать нечем: возвращается <c>null</c>, и
    /// вызывающий пропустит его, а не соберёт пустой чат.
    /// </summary>
    [Fact]
    public void Без_Адресата_Сообщение_Не_Нормализуется()
    {
        Assert.Null(TelegramBotSession.Normalize(Message(peer: null, id: 1, text: "x"), false));
    }

    /// <summary>
    /// Подпись к фото приходит тем же полем, что и текст, и обработчику нужна она
    /// как текст: иначе слово-триггер в подписи не сработал бы.
    /// </summary>
    [Fact]
    public void Подпись_Становится_Текстом()
    {
        var normalized = TelegramBotSession.Normalize(
            Message(new PeerUser { user_id = 1 }, id: 1, text: "копи"),
            isChannelPost: false
        );

        Assert.NotNull(normalized);
        Assert.Equal("копи", normalized.Text);
    }

    private static TL.Message Message(Peer? peer, int id, string text) =>
        new()
        {
            id = id,
            message = text,
            peer_id = peer,
            date = DateTime.UtcNow,
        };
}
