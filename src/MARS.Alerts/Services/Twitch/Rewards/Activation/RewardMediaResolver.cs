using MARS.Shared.Clients;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Резолв медиа награды по её данным, а не по названию.
/// </summary>
/// <remarks>
/// <para>
/// Порядок поиска повторяет монолит: сначала файл уже пришёл в событии
/// (MARS.TwitchCore знает <c>MediaInfoId</c> награды), затем — привязка по
/// <c>TwitchGuid</c>, и только потом — по цене в баллах. Цена идёт последней
/// намеренно: она не уникальна, и награда с тем же номиналом, но без привязки
/// получила бы чужой файл, если бы проверялась раньше.
/// </para>
/// <para>
/// MARS.Alerts не владеет медиа, поэтому единственный источник —
/// <see cref="IMediaStorageClient"/>. <c>null</c> от него означает «хранилище
/// недоступно», и это не то же самое, что «файла нет»: в журнал уходит
/// предупреждение, а награда всё равно активируется — иначе одна недоступная
/// зависимость гасила бы все награды сразу.
/// </para>
/// </remarks>
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
        IReadOnlyList<MediaInfo> result = reward.Media is null ? [] : [reward.Media];

        if (result.Count == 0)
        {
            var alerts = await mediaStorage.GetAllAlertsAsync(cancellationToken);

            if (alerts is null)
            {
                logger.LogWarning(
                    "Reward {RewardId}: media storage is unavailable, activation continues without media",
                    reward.RewardId
                );
            }
            else if (alerts.Count > 0)
            {
                var matched = MatchByTwitchGuid(alerts, reward.RewardId);

                if (matched is null && reward.Cost > 0)
                {
                    matched = MatchByCost(alerts, reward.Cost);
                }

                if (matched is not null)
                {
                    result = [matched];
                }
            }
        }

        return result;
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
        // Стоимость не является идентификатором награды. Legacy fallback
        // безопасен только при единственном совпадении.
        var matches = alerts
            .Where(alert =>
                alert.MetaInfo.TwitchPointsCost > 0 && alert.MetaInfo.TwitchPointsCost == cost
            )
            .Take(2)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }
}
