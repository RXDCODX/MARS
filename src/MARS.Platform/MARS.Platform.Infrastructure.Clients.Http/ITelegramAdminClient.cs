using MARS.Shared.Contracts.Telegram;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// Доставка сообщения администраторам бота.
/// </summary>
/// <remarks>
/// Бот принадлежит <c>MARS.Telegram</c>: остальные просят доставить сообщение
/// его эндпоинтом, а не держат собственного клиента Telegram.
/// </remarks>
public interface ITelegramAdminClient
{
    /// <summary>
    /// Отправляет сообщение администраторам.
    /// </summary>
    /// <returns>
    /// Итог доставки или <see langword="null"/>, если сервис отказал: не настроен,
    /// недоступен или не доставил ни одному администратору.
    /// </returns>
    Task<TelegramAdminMessageResult?> SendToAdminsAsync(
        string service,
        string message,
        CancellationToken cancellationToken = default
    );
}
