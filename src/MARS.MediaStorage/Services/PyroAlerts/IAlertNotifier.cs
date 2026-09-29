using MARS.MediaStorage.Entities;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Services.PyroAlerts;

public interface IAlertNotifier
{
    Task SendAlertAsync(MediaDto mediaDto, CancellationToken cancellationToken = default);
}
