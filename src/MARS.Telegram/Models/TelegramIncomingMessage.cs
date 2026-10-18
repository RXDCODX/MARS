namespace MARS.Telegram.Models;

/// <summary>
/// Входящее сообщение в виде, в котором его видят обработчики сервиса.
/// </summary>
/// <remarks>
/// <para>
/// Раньше обработчики принимали <c>Telegram.Bot.Types.Update</c> — тип Bot API.
/// Теперь обмен идёт по MTProto, и типы приходят другие, поэтому сообщение
/// нормализуется один раз здесь: дальше обработчики работают с чатом, текстом,
/// медиа и группой — ровно тот набор, который они действительно используют.
/// </para>
/// <para>
/// <para>
/// Сами объекты <c>TL.Photo</c> и <c>TL.Document</c> сохраняются как есть: файл
/// качается клиентом по этим объектам, и копия означала бы второе место, где надо
/// вспомнить про <c>access_hash</c> и <c>file_reference</c>.
/// </para>
/// <para>
/// Вместе с чатом несётся готовый <c>InputPeer</c>: по одному числу тип адресата
/// не восстановить — пользователь, группа и канал выглядят одинаково, а протокол
/// требует разный <c>InputPeer</c> для каждого. Строится один раз при разборе
/// обновления, и обработчики не вспоминают об этом каждый раз.
/// </para>
/// </para>
/// </remarks>
public sealed record TelegramIncomingMessage(
    long ChatId,
    TL.InputPeer Peer,
    int MessageId,
    string Text,
    bool IsChannelPost,
    long? MediaGroupId,
    TL.Photo? Photo,
    TL.Document? Document
)
{
    /// <summary>Есть ли в сообщении фото или документ.</summary>
    public bool HasMedia => Photo is not null || Document is not null;
}
