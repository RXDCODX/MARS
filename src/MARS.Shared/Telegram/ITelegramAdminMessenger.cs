namespace MARS.Shared.Telegram;

/// <summary>
/// Отправка текстового сообщения в Telegram.
/// </summary>
/// <remarks>
/// Контракт живёт в MARS.Shared, а реализация — в сервисе, который отправляет:
/// в Telegram.Bot 22.x отправка сообщения является методом расширения, а не
/// членом <c>ITelegramBotClient</c>, поэтому подменить её подменой клиента в
/// тестах нельзя. Интерфейс намеренно без упоминания Telegram.Bot — иначе
/// пакет уехал бы в каждый проект, ссылающийся на MARS.Shared.
/// </remarks>
public interface ITelegramAdminMessenger
{
    Task SendAsync(long chatId, string text, CancellationToken cancellationToken);
}
