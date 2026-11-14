namespace MARS.Shared.Contracts.Telegram;

/// <summary>
/// Запись журнала сервиса для отправки в чат Telegram.
/// </summary>
/// <remarks>
/// <para>
/// Контракт внутреннего REST-вызова <c>POST api/internal/telegram/log-entries</c>.
/// Сборщик записей живёт в каждом сервисе, а доставка — в том, которому
/// принадлежит бот.
/// </para>
/// <para>
/// Уровень и время строкой и <see cref="DateTimeOffset"/> здесь потому, что этот
/// контракт — граница транспорта, а не повторение Serilog: отправитель уже
/// привёл событие к строке, и получателю не нужно знать про <c>LogEventLevel</c>.
/// </para>
/// </remarks>
public class TelegramLogEntryRequest
{
    /// <summary>Сервис-отправитель. Пустое значение — ошибка запроса.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>Уровень записи (например <c>Error</c>).</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>Категория записи — обычно имя класса-источника.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Текст записи.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Стектрейс исключения, если запись про исключение.</summary>
    public string Exception { get; set; } = string.Empty;

    /// <summary>
    /// Время записи в UTC. <see langword="null"/> означает «время неизвестно», а не
    /// «ошибка была только что»: подмена текущим временем вводила бы в заблуждение.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>Сколько записей отправитель отбросил до этой.</summary>
    public int SuppressedCount { get; set; }
}
