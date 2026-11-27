namespace MARS.Telegram.Application.Services;

/// <summary>
/// Доставка медиа-вложений через принадлежащего сервису Telegram-бота.
/// </summary>
public interface ITelegramMediaSender
{
    Task<bool> SendMediaAsync(
        long chatId,
        string url,
        string fileName,
        string mimeType,
        string caption,
        long replyToMessageId,
        int kind,
        double duration,
        int width,
        int height,
        CancellationToken cancellationToken = default
    );
}
