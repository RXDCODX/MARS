namespace MARS.Telegram.Application.Services;

/// <summary>Порт доставки сообщений через принадлежащего сервису Telegram-бота.</summary>
public interface ITelegramMessageSender
{
    Task<int> SendToAdminsAsync(string message, CancellationToken cancellationToken);
    Task<bool> SendAsync(long chatId, string message, CancellationToken cancellationToken);
    Task<int> SendPlainToChatsAsync(
        IReadOnlyList<long> chatIds,
        string message,
        CancellationToken cancellationToken
    );
}
