using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Http;

public class RewardDefinitionsClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<RewardDefinitionsClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IRewardDefinitionsClient
{
    private const string DefinitionsUrl = "api/internal/rewards/definitions";

    public override string ServiceEndpoint => endpoints.Value.Alerts;

    public async Task<List<RewardDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = await GetAsync<OperationResult<List<RewardDefinitionSnapshot>>>(
            DefinitionsUrl,
            cancellationToken
        );

        return result?.Result ?? [];
    }
}
