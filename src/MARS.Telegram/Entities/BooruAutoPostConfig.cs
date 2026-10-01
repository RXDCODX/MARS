namespace MARS.Telegram.Entities;

/// <summary>
/// Правило автоматической публикации изображений с booru-источников.
/// </summary>
/// <remarks>
/// Таблица и данные перенесены из монолита, но код автопостинга не портирован:
/// BooruAutoPostService, BooruDiscordPoster, BooruTelegramPoster и
/// TelegramScheduleMatcher остались в MARS.Server. Пока сервиса нет, конфигурации
/// никто не читает — они хранятся ради будущего восстановления и для ручного
/// просмотра через psql.
/// TODO(Booru): восстановить конвейер автопостинга. При переносе кода учесть,
/// что ScheduledPosts ссылаются на эту таблицу каскадом.
/// </remarks>
public class BooruAutoPostConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public BooruSource Source { get; set; }

    public BooruTargetPlatform TargetPlatform { get; set; }

    /// <summary>
    /// Канал Discord назначения. В монолите varchar(64); длина ограничена
    /// явно, иначе обрезка молча прошла бы уже в базе.
    /// </summary>
    public string DiscordChannelId { get; set; } = string.Empty;

    public long? TelegramChannelId { get; set; }

    /// <summary>Сколько публикаций выполнить по этому правилу.</summary>
    public int TargetPostCount { get; set; }

    /// <summary>Пост конкретного номера вместо выборки по тегам.</summary>
    public int? SpecificPostId { get; set; }

    public string Tags { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public int PlanningHorizonDays { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>Шаблон текста публикации.</summary>
    public string Message { get; set; } = string.Empty;

    public BooruTelegramParseMode TelegramParseMode { get; set; }
    public DateTime? LastExecutedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
