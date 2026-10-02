namespace MARS.Videos365.Services;

/// <summary>
/// Отправка текстового сообщения администратору.
/// </summary>
/// <remarks>
/// Seam над <c>ITelegramBotClient</c>: в Telegram.Bot 22.x отправка сообщения —
/// метод расширения, а не член интерфейса, поэтому подменить её подменой клиента
/// в тестах нельзя.
/// </remarks>
public interface ITelegramAdminMessenger
{
    Task SendAsync(long chatId, string text, CancellationToken cancellationToken);
}