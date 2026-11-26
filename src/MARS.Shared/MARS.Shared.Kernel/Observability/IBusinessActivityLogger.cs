namespace MARS.Shared.Kernel.Observability;

/// <summary>
/// Записывает бизнес-активности в общий структурированный журнал.
/// </summary>
public interface IBusinessActivityLogger
{
    /// <summary>
    /// Записывает одну бизнес-активность.
    /// </summary>
    void Log(BusinessActivity activity);

    /// <summary>
    /// Записывает бизнес-активность с исключением.
    /// </summary>
    void Log(BusinessActivity activity, Exception exception);
}
