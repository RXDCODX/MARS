using System;
using System.Threading;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Twitch.Rest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Twitch.Infrastructure.Http;

public class TwitchUsersClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<TwitchUsersClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ITwitchUsersClient
{
    public override string ServiceEndpoint => endpoints.Value.TwitchCore;

    public async Task<TwitchUserRestModel?> GetFullUserByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        return await GetAsync<TwitchUserRestModel>(
            $"api/TwitchUsers/by-login-full/{Uri.EscapeDataString(login)}",
            cancellationToken
        );
    }
}
