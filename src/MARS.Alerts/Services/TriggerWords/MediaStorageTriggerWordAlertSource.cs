using MARS.Shared.Clients;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.TriggerWords;

/// <summary>
/// Источник алертов через внутренний API MARS.MediaStorage.
/// </summary>
public sealed class MediaStorageTriggerWordAlertSource(IMediaStorageClient client)
    : ITriggerWordAlertSource
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
