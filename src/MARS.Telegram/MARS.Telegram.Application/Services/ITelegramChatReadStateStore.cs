using MARS.Telegram.Domain.Entities;

namespace MARS.Telegram.Application.Services;

/// <summary>
/// Хранилище меток «прочитано» для входящих сообщений Telegram.
/// </summary>
public interface ITelegramChatReadStateStore
{
    /// <summary>Загружает все метки: идентификатор чата → последний обработанный id.</summary>
    Task<IReadOnlyDictionary<long, int>> LoadAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Отмечает чат как прочитанный до указанного идентификатора. Значение
    /// только растёт: более старое значение не затирает более новое.
    /// </summary>
    Task MarkReadAsync(
        long chatId,
        int peerKind,
        int messageId,
        DateTimeOffset at,
        CancellationToken cancellationToken
    );
}
