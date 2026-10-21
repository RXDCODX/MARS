namespace MARS.Telegram.Services.Logging;

/// <summary>
/// Лимит частоты доставки журнала — на сервис.
/// </summary>
/// <remarks>
/// <para>
/// Считается здесь, а не в отправителях: там видны только свои записи, и
/// суммарный поток не виден никому. Одна точка может решить, что чат
/// наполняется слишком быстро, — и это как раз получатель.
/// </para>
/// <para>
/// Лимит на сервис, а не общий: иначе один зашумевший сервис молча отрезал бы
/// журнал остальным.
/// </para>
/// <para>
/// Окно фиксированное, а не скользящее: разница видна только на границе
/// минуты, а поведение предсказуемо и проверяемо.
/// </para>
/// </remarks>
public sealed class TelegramLogRateLimiter
{
    /// <summary>Лимит по умолчанию: пять записей в минуту на сервис.</summary>
    public const int DefaultLimitPerMinute = 5;

    private readonly int _limitPerMinute;
    private readonly Dictionary<string, Window> _windows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _sync = new();

    /// <summary>
    /// Создаёт лимитер.
    /// </summary>
    /// <param name="limitPerMinute">
    /// Сколько записей сервиса проходит за минуту. Ноль или меньше означает
    /// «не ограничивать»: опечатка в конфигурации не должна молча отключить
    /// журнал.
    /// </param>
    public TelegramLogRateLimiter(int limitPerMinute)
    {
        _limitPerMinute = limitPerMinute;
    }

    /// <summary>
    /// Пытается пропустить запись сервиса.
    /// </summary>
    /// <param name="service">Имя сервиса-отправителя.</param>
    /// <param name="now">Текущий момент.</param>
    public TelegramLogRateLimitDecision TryAcquire(string service, DateTimeOffset now)
    {
        lock (_sync)
        {
            var key = string.IsNullOrWhiteSpace(service) ? "без сервиса" : service;

            if (!_windows.TryGetValue(key, out var window))
            {
                window = new Window(now);
                _windows[key] = window;
            }
            else if (now - window.Start >= TimeSpan.FromMinutes(1))
            {
                // Новое окно: счётчик принятых записей обнуляется, а счётчик
                // отброшенных — нет. Отброшенные записи относятся к предыдущей
                // принятой, и потерять их счётчик не должен.
                window = new Window(now) { Suppressed = window.Suppressed };
                _windows[key] = window;
            }

            if (_limitPerMinute <= 0 || window.Accepted < _limitPerMinute)
            {
                window.Accepted++;
                var decision = new TelegramLogRateLimitDecision(true, window.Suppressed);
                window.Suppressed = 0;

                return decision;
            }

            window.Suppressed++;

            return new TelegramLogRateLimitDecision(false, window.Suppressed);
        }
    }

    /// <summary>
    /// Состояние сервиса: начало окна, сколько записей в него принято и сколько
    /// отброшено с прошлой принятой.
    /// </summary>
    private sealed class Window(DateTimeOffset start)
    {
        public DateTimeOffset Start { get; set; } = start;

        public int Accepted { get; set; }

        public int Suppressed { get; set; }
    }
}
