using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Downloads;

public interface IMediaDownloadJobProcessor
{
    Task ProcessAsync(MediaDownloadJob job, CancellationToken cancellationToken = default);
}