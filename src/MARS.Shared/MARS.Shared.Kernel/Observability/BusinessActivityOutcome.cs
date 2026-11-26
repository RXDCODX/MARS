namespace MARS.Shared.Kernel.Observability;

/// <summary>
/// Исход бизнес-активности. Используется в структурированных логах и метриках,
/// поэтому значения стабильны и не локализуются.
/// </summary>
public enum BusinessActivityOutcome
{
    /// <summary>Операция принята к обработке.</summary>
    Accepted,

    /// <summary>Операция успешно завершена.</summary>
    Succeeded,

    /// <summary>Операция отклонена по бизнес-правилам.</summary>
    Rejected,

    /// <summary>Операция завершилась ошибкой.</summary>
    Failed,

    /// <summary>Операция отменена.</summary>
    Cancelled,
}
