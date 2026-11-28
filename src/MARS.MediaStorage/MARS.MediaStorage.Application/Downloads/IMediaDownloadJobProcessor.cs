using MARS.MediaStorage.Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace MARS.MediaStorage.Application.Downloads;

public interface IMediaDownloadJobProcessor
{
    Task ProcessAsync(MediaDownloadJob job, CancellationToken cancellationToken = default);
}
