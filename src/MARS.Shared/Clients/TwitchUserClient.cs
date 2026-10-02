using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// HTTP-клиент справочника пользователей Twitch в MARS.TwitchCore.
/// Эндпоинт лежит под правилом YARP <c>twitch-core-users</c>.
/// </summary>
public class TwitchUserClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<TwitchUserClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ITwitchUserClient
{
    public override string ServiceEndpoint => endpoints.Value.TwitchCore;

    public Task<string?> ResolveIdByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<string>(
            $"api/TwitchUsers/by-login/{Uri.EscapeDataString(login.Trim().TrimStart('@'))}",
            cancellationToken
        );
    }
}
