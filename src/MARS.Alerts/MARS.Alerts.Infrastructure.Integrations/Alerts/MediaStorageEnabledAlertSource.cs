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

        var result = alerts
            .Where(alert =>
                alert.MetaInfo.IsEnabled && !string.IsNullOrWhiteSpace(alert.TextInfo.TriggerWord)
            )
            .ToArray();

        return result;
    }
}
