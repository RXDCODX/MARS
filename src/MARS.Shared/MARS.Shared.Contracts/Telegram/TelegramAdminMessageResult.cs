namespace MARS.Shared.Contracts.Telegram;

/// <summary>
/// Итог доставки сообщения администраторам.
/// </summary>
/// <remarks>
/// Доставка best effort: один недоступный администратор не лишает остальных
/// сообщения. Ноль получателей означает ошибку доставки, и вызывающий узнаёт о
/// ней HTTP-статусом, а не нулевым числом молча.
/// </remarks>
public class TelegramAdminMessageResult
{
    /// <summary>Сколько администраторов получило сообщение.</summary>
    public int Recipients { get; set; }
}
