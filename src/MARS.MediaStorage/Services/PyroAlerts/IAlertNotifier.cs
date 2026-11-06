using MARS.MediaStorage.Entities;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Services.PyroAlerts;

public interface IAlertNotifier
{
    Task SendAlertAsync(MediaDto mediaDto, CancellationToken cancellationToken = default);
}
