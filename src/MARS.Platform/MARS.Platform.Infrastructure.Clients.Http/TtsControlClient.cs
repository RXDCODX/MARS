using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Models.Control;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Clients.Http;

/// <summary>
/// HTTP-клиент управления глушением TTS в MARS.TTS.
/// </summary>
public class TtsControlClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<TtsControlClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ITtsControlClient
{
    private const string MuteStateUrl = "api/internal/tts/mute-state";

    public override string ServiceEndpoint => endpoints.Value.TTS;

    public Task<bool> SetMuteStateAsync(
        bool isMuted,
        double volume,
        CancellationToken cancellationToken = default
    )
    {
        return PostAsync(
            MuteStateUrl,
            new TtsMuteStateRequest { IsMuted = isMuted, Volume = volume },
            cancellationToken
        );
    }
}
