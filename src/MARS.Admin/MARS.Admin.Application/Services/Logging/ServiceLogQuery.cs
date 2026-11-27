namespace MARS.Admin.Application.Services.Logging;

/// <summary>
/// Запрос к журналу Loki.
/// </summary>
public sealed record ServiceLogQuery
{
    public string? Service { get; init; }

    public string? Level { get; init; }

    public string? Search { get; init; }

    public string? ActivityType { get; init; }

    public string? Platform { get; init; }

    public string? Outcome { get; init; }

    public bool BusinessOnly { get; init; }

    public int Hours { get; init; } = 1;

    public int Limit { get; init; } = 100;
}
