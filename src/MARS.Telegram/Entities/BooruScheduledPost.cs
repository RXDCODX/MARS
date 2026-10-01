namespace MARS.Telegram.Entities;

/// <summary>
/// Отложенная публикация по правилу <see cref="BooruAutoPostConfig"/>.
/// </summary>
/// <remarks>
/// Перенесена из монолита вместе с конфигурациями. Кода, который создаёт и
/// исполняет эти записи, в новом репозитории нет (см.
/// <see cref="BooruAutoPostConfig"/>), поэтому таблица пока только хранит
/// расписание. Сами строки восстановимы из cron-выражений конфигураций, но
/// переносятся, чтобы состояние «что уже запланировано» не потерялось.
/// TODO(Booru): восстановить планировщик поверх этих таблиц.
/// </remarks>
public class BooruScheduledPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Правило-владелец. Ссылка каскадная: удаление правила удаляет его
    /// расписание (как и в монолите).
    /// </summary>
    public Guid ConfigId { get; set; }

    public BooruAutoPostConfig? Config { get; set; }

    public BooruSource Source { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public BooruScheduledPostStatus Status { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
