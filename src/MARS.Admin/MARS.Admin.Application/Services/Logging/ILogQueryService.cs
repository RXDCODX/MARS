using MARS.Admin.Application.Services.ServiceManager;

namespace MARS.Admin.Application.Services.Logging;

/// <summary>
/// Чтение технических и бизнес-логов из Loki.
/// </summary>
public interface ILogQueryService
{
    Task<IReadOnlyList<ServiceLog>> QueryAsync(
        ServiceLogQuery query,
        CancellationToken cancellationToken = default
    );
}
