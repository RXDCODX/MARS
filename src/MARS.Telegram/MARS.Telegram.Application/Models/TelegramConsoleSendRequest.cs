namespace MARS.Telegram.Application.Models;

/// <summary>
/// Запрос на отправку сообщения администраторам из страницы консоли.
/// </summary>
public sealed class TelegramConsoleSendRequest
{
    public string Text { get; set; } = string.Empty;
}
