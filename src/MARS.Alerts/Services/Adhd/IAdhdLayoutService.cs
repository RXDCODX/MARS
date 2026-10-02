using MARS.Alerts.Models;
using MARS.Shared.Models;

namespace MARS.Alerts.Services.Adhd;

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
