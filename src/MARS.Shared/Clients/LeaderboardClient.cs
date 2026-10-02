using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// HTTP-клиент таблицы лидеров мини-игр в MARS.TwitchCore.
/// Эндпоинты закрыты политикой <c>ServiceApiKey</c>, ключ подставляет
/// <see cref="ServiceHttpClientBase"/>.
/// </summary>
public class LeaderboardClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<LeaderboardClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), ILeaderboardClient
{
    public override string ServiceEndpoint => endpoints.Value.TwitchCore;

    public Task<LeaderboardTop?> GetTopAsync(
        int count,
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<LeaderboardTop>($"api/leaderboard/top?count={count}", cancellationToken);
    }

    public Task<LeaderboardStats?> GetUserStatsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<LeaderboardStats>(
            $"api/leaderboard/user/{Uri.EscapeDataString(twitchId)}",
            cancellationToken
        );
    }
}
