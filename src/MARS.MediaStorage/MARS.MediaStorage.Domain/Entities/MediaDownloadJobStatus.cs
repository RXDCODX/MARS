using System.Diagnostics.CodeAnalysis;

namespace MARS.MediaStorage.Domain.Entities;

public enum MediaDownloadJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}
