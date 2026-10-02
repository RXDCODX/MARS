using MARS.Shared.Configuration;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// HTTP-клиент MARS.MediaStorage. Ответ <c>GET /api/MediaInfo/{id}</c> — конверт
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
        return GetAsync<MediaInfo>($"api/MediaInfo/{mediaInfoId}", cancellationToken);
    }

    public async Task<IReadOnlyList<MediaInfo>?> GetAllAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var alerts = await GetAsync<List<MediaInfo>>("api/MediaInfo", cancellationToken);

        return alerts;
    }
}
