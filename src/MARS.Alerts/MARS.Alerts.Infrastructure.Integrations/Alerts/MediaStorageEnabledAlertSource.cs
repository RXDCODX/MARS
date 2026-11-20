using MARS.Alerts.Application.Alerts;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Alerts.Infrastructure.Integrations.Alerts;

/// <summary>
/// Источник алертов через внутренний API MARS.MediaStorage.
/// </summary>
public sealed class MediaStorageEnabledAlertSource(IMediaStorageClient client) : IEnabledAlertSource
{
    public async Task<IReadOnlyList<MediaInfo>?> GetEnabledAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var alerts = await client.GetAllAlertsAsync(cancellationToken);

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
