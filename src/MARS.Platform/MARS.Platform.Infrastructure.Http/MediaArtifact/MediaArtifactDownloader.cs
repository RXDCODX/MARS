using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Http.MediaArtifact;

public sealed class MediaArtifactDownloader(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceAuthOptions> authOptions,
    ILogger<MediaArtifactDownloader> logger
) : IMediaArtifactDownloader
{
    private readonly ILogger<MediaArtifactDownloader> _logger = logger;

    public const string HttpClientName = "mars-media-artifact";

    public async Task<Stream> DownloadAsync(
        string url,
        long maxBytes,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Artifact URL is required.", nameof(url));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyApiKey(request);

        using var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Artifact download failed with {(int)response.StatusCode}."
            );
        }

        if (
            response.Content.Headers.ContentLength is long contentLength
            && contentLength > maxBytes
        )
        {
            throw new InvalidOperationException(
                $"Artifact is larger than allowed limit ({maxBytes} bytes)."
            );
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new MaxBytesStream(stream, maxBytes);
    }

    private void ApplyApiKey(HttpRequestMessage request)
    {
        var options = authOptions.Value;
        if (options.IsEnabled)
        {
            request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
        }
    }

    private sealed class MaxBytesStream(Stream inner, long maxBytes) : Stream
    {
        private long _readBytes;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        )
        {
            var read = await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            _readBytes += read;
            if (_readBytes > maxBytes)
            {
                throw new InvalidOperationException($"Artifact exceeded {maxBytes} bytes.");
            }

            return read;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            return ReadValueTaskAsync(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadValueTaskAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken
        )
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            _readBytes += read;
            if (_readBytes > maxBytes)
            {
                throw new InvalidOperationException($"Artifact exceeded {maxBytes} bytes.");
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
