namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Итог доставки записи журнала.
/// </summary>
public sealed class TelegramLogPublishResult
{
    /// <summary>Сколько чатов получили запись.</summary>
    public int Recipients { get; set; }

    /// <summary>
    /// Сколько записей сервиса было отброшено лимитом частоты на стороне
    /// Telegram после предыдущей принятой.
    /// </summary>
    public int Suppressed { get; set; }
}
