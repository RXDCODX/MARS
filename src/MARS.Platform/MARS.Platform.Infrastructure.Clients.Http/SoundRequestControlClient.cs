using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// HTTP-клиент управления глушением плеера MARS.SoundRequest.
/// </summary>
public class SoundRequestControlClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<SoundRequestControlClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ISoundRequestControlClient
{
    private const string MuteUrl = "api/internal/sound-request/mute";
    private const string UnmuteUrl = "api/internal/sound-request/unmute";

    public override string ServiceEndpoint => endpoints.Value.SoundRequest;

    public Task<bool> MuteAllAsync(CancellationToken cancellationToken = default)
    {
        return PostAsync(MuteUrl, cancellationToken);
    }

    public Task<bool> UnmuteAllAsync(CancellationToken cancellationToken = default)
    {
        return PostAsync(UnmuteUrl, cancellationToken);
    }
}
