using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Application.Alerts;

public interface IAlertNotifier
{
    Task SendAlertAsync(MediaDto mediaDto, CancellationToken cancellationToken = default);
}
