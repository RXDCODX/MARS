namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Запись журнала, готовая к доставке в Telegram.
/// </summary>
/// <remarks>
/// Запись собирается в сервисе-отправителе, а форматируется и отправляется в
/// <c>MARS.Telegram</c>: собирать её в получателе нельзя, потому что получатель
/// видит только готовые строки, а разбирать шаблоны Serilog на стороне
/// получателя — значит тащить туда Serilog.
/// </remarks>
public sealed class TelegramLogEntry
{
    /// <summary>Имя сервиса-отправителя.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>Уровень записи: Verbose, Debug, Information, Warning, Error, Fatal.</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>Категория записи (SourceContext): класс или источник события.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Текст записи без разметки.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Текст исключения со стектрейсом. Пусто, если исключения не было.</summary>
    public string Exception { get; set; } = string.Empty;

    /// <summary>Момент записи в UTC.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Сколько записей отправитель потерял локально перед этой.
    /// </summary>
    public int SuppressedCount { get; set; }
}
