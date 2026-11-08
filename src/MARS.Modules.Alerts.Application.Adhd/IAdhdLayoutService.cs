using MARS.Modules.Alerts.Application.Adhd.Models;
using MARS.Shared.Kernel;

namespace MARS.Modules.Alerts.Application.Adhd;

/// <summary>
/// Чтение и запись настройки раскладки ADHD-экрана.
/// </summary>
public interface IAdhdLayoutService
{
    Task<OperationResult<AdhdLayoutConfigDto>> GetAsync(CancellationToken cancellationToken);

    Task<OperationResult<AdhdLayoutConfigDto>> UpdateAsync(
        AdhdLayoutConfigDto config,
        CancellationToken cancellationToken
    );
}
