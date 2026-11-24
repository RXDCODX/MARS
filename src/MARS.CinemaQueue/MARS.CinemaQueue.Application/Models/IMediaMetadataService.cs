using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MARS.CinemaQueue.Application.Models;

public interface IMediaMetadataService
{
    Task<MediaMetadata?> GetMetadataAsync(
        string url,
        CancellationToken cancellationToken = default
    );
}
