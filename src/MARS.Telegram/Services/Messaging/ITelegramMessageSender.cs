namespace MARS.Telegram.Services.Messaging;

/// <summary>
/// Отправка сообщений от имени бота.
/// </summary>
/// <remarks>
/// Абстракция нужна не для красоты: уведомление о переавторизации Twitch шлёт
/// <c>MARS.TwitchCore</c> по gRPC, и внутри сервиса отправка одна — через бота.
/// С Bot API это был вызов расширения <c>SendMessage</c>, который нельзя было
/// подменить заглушкой, и проверка упиралась в сеть.
/// </remarks>
public interface ITelegramMessageSender
{
    /// <summary>
    /// Отправляет сообщение всем администраторам.
    /// </summary>
    /// <returns>Сколько администраторов получили сообщение.</returns>
    Task<int> SendToAdminsAsync(string message, CancellationToken cancellationToken);

    /// <summary>
    /// Отправляет сообщение в указанный чат.
    /// </summary>
    Task<bool> SendAsync(long chatId, string message, CancellationToken cancellationToken);

    /// <summary>
    /// Отправляет сообщение в указанные чаты как обычный текст, без разметки.
    /// </summary>
    /// <remarks>
    /// Без разметки — потому что журнал содержит произвольный текст ошибки, а
    /// наш разбор считает «&lt;» рядом с буквой началом тега и отказывается
    /// отправлять сообщение вовсе. Ошибка доставки потеряла бы ту самую
    /// ошибку, ради которой доставлялась.
    /// </remarks>
    /// <returns>Сколько чатов получили сообщение.</returns>
    Task<int> SendPlainToChatsAsync(
        IReadOnlyList<long> chatIds,
        string message,
        CancellationToken cancellationToken
    );
}
