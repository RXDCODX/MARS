namespace MARS.Telegram.Application.Models;

/// <summary>
/// Одна запись о сообщении, полученном ботом.
/// </summary>
/// <remarks>
/// Память, а не база: администратору нужен только свежий срез, а полный поток
/// он и так видит пересылкой в Telegram. Хранить историю в БД означало бы ещё
/// одну таблицу и ещё одну миграцию ради страницы консоли.
/// </remarks>
public sealed record TelegramInboxEntry(
    DateTime ReceivedAtUtc,
    long ChatId,
    int MessageId,
    string Text,
    bool IsChannelPost,
    bool HasMedia
);
