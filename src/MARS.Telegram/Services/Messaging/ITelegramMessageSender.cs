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
}
