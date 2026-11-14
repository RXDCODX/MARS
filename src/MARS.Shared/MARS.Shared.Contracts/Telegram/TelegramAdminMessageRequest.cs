namespace MARS.Shared.Contracts.Telegram;

/// <summary>
/// Просьба доставить сообщение администраторам бота.
/// </summary>
/// <remarks>
/// Контракт внутреннего REST-вызова <c>POST api/internal/telegram/admin-messages</c>.
/// Бот принадлежит одному сервису, поэтому остальные просят доставить сообщение
/// его эндпоинтом, а не держат собственного клиента Telegram.
/// </remarks>
public class TelegramAdminMessageRequest
{
    /// <summary>Сервис, который просит доставить сообщение. Попадает в журнал.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>Текст сообщения в разметке Telegram.</summary>
    public string Message { get; set; } = string.Empty;
}
