using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MARS.Platform.Infrastructure.Http.MediaArtifact;

public interface IMediaArtifactDownloader
{
    Task<Stream> DownloadAsync(
        string url,
        long maxBytes,
        CancellationToken cancellationToken = default
    );
}
