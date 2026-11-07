using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MARS.CinemaQueue.Services;

public interface IMediaMetadataService
{
    Task<MediaMetadata?> GetMetadataAsync(
        string url,
        CancellationToken cancellationToken = default
    );
}

public class MediaMetadata
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
}
