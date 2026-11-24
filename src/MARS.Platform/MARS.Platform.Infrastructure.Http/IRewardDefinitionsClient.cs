using MARS.Shared.Contracts.Alerts;
using Microsoft.Extensions.Logging;

namespace MARS.Platform.Infrastructure.Http;

/// <summary>
/// Получение определений наград из MARS.Alerts (internal, service-to-service).
/// </summary>
public interface IRewardDefinitionsClient
{
    Task<List<RewardDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken cancellationToken = default
    );
}
