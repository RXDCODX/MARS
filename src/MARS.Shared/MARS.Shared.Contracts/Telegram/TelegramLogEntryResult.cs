namespace MARS.Shared.Contracts.Telegram;

/// <summary>
/// Итог приёма записи журнала.
/// </summary>
/// <remarks>
/// <see cref="Suppressed"/> заполняется и при отказе лимита частоты: тогда
/// <see cref="Recipients"/> равно нулю, а вызывающий узнаёт, что запись не
/// потерялась молча.
/// </remarks>
public class TelegramLogEntryResult
{
    /// <summary>Сколько чатов получило запись.</summary>
    public int Recipients { get; set; }

    /// <summary>Сколько записей отброшено лимитом частоты до этой.</summary>
    public int Suppressed { get; set; }
}
