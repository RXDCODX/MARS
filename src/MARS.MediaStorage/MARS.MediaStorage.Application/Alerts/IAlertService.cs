using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.Http;

namespace MARS.MediaStorage.Application.Alerts;

public interface IAlertService
{
    Task<IReadOnlyList<MediaInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MediaInfo?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AlertFileLookup> GetFileAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AlertSaveResult> CreateAsync(
        MediaInfo alert,
        IFormFile? file,
        CancellationToken cancellationToken = default
    );

    Task<AlertSaveResult> UpdateAsync(
        Guid id,
        MediaInfo alert,
        IFormFile? file,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
