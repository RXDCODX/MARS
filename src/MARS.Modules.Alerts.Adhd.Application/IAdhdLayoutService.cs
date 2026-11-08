using MARS.Modules.Alerts.Adhd.Application.Models;
using MARS.Shared.Kernel;

namespace MARS.Modules.Alerts.Adhd.Application;

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
