namespace MARS.Shared.Kernel.Observability;

/// <summary>
/// Единый описатель бизнес-активности для структурированного журнала.
/// Текст <see cref="Summary" /> должен быть коротким, однострочным и читаемым
/// человеком; секреты и полные DTO в него не передаются.
/// </summary>
public sealed record BusinessActivity
{
    /// <summary>Широкая предметная область: chat, command, reward, sound-request.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Стабильное действие: message-received, command-executed, reward-redeemed.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Читаемый итог активности, без префикса сервиса и автора.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Исход активности.</summary>
    public BusinessActivityOutcome Outcome { get; init; } = BusinessActivityOutcome.Succeeded;

    /// <summary>Тип автора: user, admin, system, service.</summary>
    public string? ActorType { get; init; }

    /// <summary>Внутренний идентификатор автора, если он есть.</summary>
    public string? ActorId { get; init; }

    /// <summary>Отображаемое имя автора.</summary>
    public string? ActorDisplayName { get; init; }

    /// <summary>Платформа, с которой инициирована активность.</summary>
    public string? Platform { get; init; }

    /// <summary>Тип объекта, над которым выполнено действие.</summary>
    public string? SubjectType { get; init; }

    /// <summary>Идентификатор объекта, над которым выполнено действие.</summary>
    public string? SubjectId { get; init; }

    /// <summary>Идентификатор запроса или операции для корреляции.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Длительность выполнения в миллисекундах.</summary>
    public double? DurationMs { get; init; }

    /// <summary>Дополнительные низкокардинальные/строковые метаданные.</summary>
    public IReadOnlyDictionary<string, string?>? Metadata { get; init; }

    /// <summary>Необязательный уровень записи. По умолчанию выводится из исхода.</summary>
    public BusinessActivityLevel Level { get; init; } = BusinessActivityLevel.Information;
}
