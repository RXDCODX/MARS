namespace MARS.Shared.Development;

/// <summary>
/// Результат одного внешнего процесса.
/// </summary>
/// <param name="ExitCode">Код возврата; <c>-1</c> — процесс не выполнился.</param>
/// <param name="StandardOutput">stdout целиком.</param>
/// <param name="StandardError">stderr целиком.</param>
/// <param name="TimedOut">
/// Истёк предельный срок: процесс был остановлен принудительно.
/// </param>
/// <param name="Failure">
/// Причина, по которой процесс не удалось выполнить: например, в PATH нет
/// <c>docker</c>. <c>null</c>, когда процесс стартовал.
/// </param>
public sealed record DevProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false,
    string? Failure = null
)
{
    /// <summary>Успех: процесс стартовал, не истёк срок и вернул ноль.</summary>
    public bool Succeeded => ExitCode == 0 && !TimedOut && Failure is null;

    /// <summary>Причина отказа одной строкой: таймаут, не запустился или код возврата.</summary>
    public string Reason
    {
        get
        {
            var reason =
                Failure is not null ? Failure
                : TimedOut ? "истёк предельный срок выполнения"
                : $"код возврата {ExitCode}";

            return reason;
        }
    }
}
