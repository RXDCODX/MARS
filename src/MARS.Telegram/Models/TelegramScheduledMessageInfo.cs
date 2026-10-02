namespace MARS.Telegram.Models;

/// <summary>
/// Отложенное сообщение Telegram, найденное при сверке расписания.
/// </summary>
/// <remarks>
/// Не сущность: это снимок из WTelegram, который планировщик сравнивает с
/// вхождениями CRON. Идентификатор сообщения нужен для отмены уже
/// запланированной публикации.
/// </remarks>
public record TelegramScheduledMessageInfo(int MessageId, DateTime ScheduledAtUtc);
