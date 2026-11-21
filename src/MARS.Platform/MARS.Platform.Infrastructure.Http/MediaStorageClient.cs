using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Http;

/// <summary>
/// HTTP-клиент MARS.MediaStorage. Ответ <c>GET /api/MediaInfoApi/{id}</c> — конверт
/// <c>OperationResult&lt;ApiMediaInfo&gt;</c>, а <c>ApiMediaInfo</c> наследует
/// общий <see cref="MediaInfo"/>, поэтому десериализация идёт прямо в контракт.
/// </summary>
public class MediaStorageClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<MediaStorageClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IMediaStorageClient
{
    public override string ServiceEndpoint => endpoints.Value.MediaStorage;

    public Task<MediaInfo?> GetMediaInfoAsync(
        Guid mediaInfoId,
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<MediaInfo>($"api/MediaInfoApi/{mediaInfoId}", cancellationToken);
    }

    public Task<MediaInfo?> GetNextRandomMemeAsync(
        string folderPath,
        string? displayName,
        CancellationToken cancellationToken = default
    )
    {
        var query = $"api/RandomMeme/next?folderPath={Uri.EscapeDataString(folderPath)}";

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            query += $"&displayName={Uri.EscapeDataString(displayName)}";
        }

        return GetAsync<MediaInfo>(query, cancellationToken);
    }

    public async Task<IReadOnlyList<MediaInfo>?> GetAllAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var alerts = await GetAsync<List<MediaInfo>>("api/MediaInfoApi", cancellationToken);

        return alerts;
    }

    public async Task<string?> UploadMemoryFileAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default
    )
    {
        var request = new MemoryFileUploadRequest
        {
            FileName = fileName,
            ContentBase64 = Convert.ToBase64String(content),
        };

        var result = await PostAsync<MemoryFileUploadRequest, MemoryFileUploadResult>(
            "api/internal/media/memory-files",
            request,
            cancellationToken
        );

        return result?.Path;
    }
}
