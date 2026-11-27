using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Alerts.Application.Alerts;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Alerts.Infrastructure.Alerts;

/// <summary>
/// Источник алертов через внутренний API MARS.MediaStorage.
/// </summary>
public sealed class MediaStorageEnabledAlertSource(IMarsGrpcClient grpcClient) : IEnabledAlertSource
{
    public async Task<IReadOnlyList<MediaInfo>?> GetEnabledAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<MediaInfo>? alerts;
        try
        {
            alerts = await grpcClient.MediaStorage.GetAllAlertsAsync(cancellationToken);
        }
        catch (RpcException)
        {
            return null;
        }

        if (alerts is null)
        {
            return null;
        }

        // Источник отдаёт ВСЕ включённые алерты: триггер-слово проверяется
        // TriggerWordMatcher (только там это имеет смысл), а алерты, привязанные
        // к наградам (TwitchGuid/TwitchPointsCost), триггера не имеют, но тоже должны
        // показываться по событию погашения награды (RewardBoundAlertDispatcher).
        var result = alerts.Where(alert => alert.MetaInfo.IsEnabled).ToArray();

        return result;
    }
}
