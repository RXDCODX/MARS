using MARS.Shared.Clients;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Резолвит медиа награды по точной локальной привязке, затем по legacy-метаданным.
/// </summary>
public sealed class RewardMediaResolver(
    IMediaStorageClient mediaStorage,
    ILogger<RewardMediaResolver> logger
) : IRewardMediaResolver
{
    public async Task<IReadOnlyList<MediaInfo>> ResolveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken = default
    )
    {
        if (reward.Media is not null)
        {
            return [reward.Media];
        }

        // Главная привязка: Twitch reward ID → локальная запись → MediaInfoId.
        if (reward.MediaInfoId is { } mediaInfoId && mediaInfoId != Guid.Empty)
        {
            try
            {
                var linkedMedia = await mediaStorage.GetMediaInfoAsync(
                    mediaInfoId,
                    cancellationToken
                );

                if (linkedMedia is not null)
                {
                    return [linkedMedia];
                }
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "Reward {RewardId}: linked media {MediaInfoId} is unavailable; trying legacy matching",
                    reward.RewardId,
                    mediaInfoId
                );
            }
        }

        IReadOnlyList<MediaInfo>? alerts;

        try
        {
            alerts = await mediaStorage.GetAllAlertsAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Reward {RewardId}: media storage failed; activation continues without media",
                reward.RewardId
            );
            return [];
        }

        if (alerts is null)
        {
            logger.LogWarning(
                "Reward {RewardId}: media storage is unavailable, activation continues without media",
                reward.RewardId
            );
            return [];
        }

        if (alerts.Count == 0)
        {
            return [];
        }

        var matched = MatchByTwitchGuid(alerts, reward.RewardId);

        if (matched is null && reward.Cost > 0)
        {
            matched = MatchByCost(alerts, reward.Cost);
        }

        return matched is null ? [] : [matched];
    }

    private static MediaInfo? MatchByTwitchGuid(IReadOnlyList<MediaInfo> alerts, string rewardId)
    {
        MediaInfo? match = null;

        if (Guid.TryParse(rewardId, out var rewardGuid) && rewardGuid != Guid.Empty)
        {
            match = alerts.FirstOrDefault(alert =>
                alert.MetaInfo.TwitchGuid is { } guid && guid != Guid.Empty && guid == rewardGuid
            );
        }

        return match;
    }

    private static MediaInfo? MatchByCost(IReadOnlyList<MediaInfo> alerts, int cost)
    {
        // Стоимость не является идентификатором. Legacy fallback безопасен,
        // только если совпадение ровно одно.
        var matches = alerts
            .Where(alert =>
                alert.MetaInfo.TwitchPointsCost > 0 && alert.MetaInfo.TwitchPointsCost == cost
            )
            .Take(2)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }
}
