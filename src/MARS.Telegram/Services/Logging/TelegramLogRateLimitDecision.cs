namespace MARS.Telegram.Services.Logging;

/// <summary>
/// Решение лимита частоты: пропустить запись или отбросить.
/// </summary>
/// <param name="Allowed">Пропустить ли запись.</param>
/// <param name="Suppressed">
/// Сколько записей этого сервиса было отброшено после предыдущей принятой.
/// </param>
public sealed record TelegramLogRateLimitDecision(bool Allowed, int Suppressed);
