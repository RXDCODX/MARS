using MARS.Shared.Contracts.Rewards;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public class RewardBindingSyncService(
    IChannelRewardsService channelRewardsService,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ILogger<RewardBindingSyncService> logger
) : IRewardBindingSyncService
{
    public async Task<RewardBindingReport> SyncAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var local = await db.ChannelRewards.ToListAsync(cancellationToken);
        IEnumerable<CustomReward>? remoteRaw = null;
        try
        {
            remoteRaw = await channelRewardsService.GetRewardsAsync();
        }
        catch
        {
            remoteRaw = [];
        }

        var remote = (remoteRaw ?? [])
            .Select(reward => new RemoteReward(reward.Id, reward.Title, reward.Cost))
            .ToList();

        foreach (var definition in RewardCatalog.All)
        {
            var hasRecord = local.Any(record =>
                string.Equals(record.RewardCode, definition.Code, StringComparison.Ordinal)
                || (
                    string.IsNullOrWhiteSpace(record.RewardCode)
                    && string.Equals(
                        record.Title,
                        definition.LegacyTitle,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

            if (hasRecord)
            {
                continue;
            }

            var record = new ChannelRewardRecord
            {
                Title = definition.LegacyTitle,
                Cost = definition.ExpectedCost,
                RewardCode = definition.Code,
                NormalizedEmoji = definition.NormalizedEmoji,
                BindingStatus = nameof(RewardBindingStatus.Orphaned),
                IsEnabled = true,
            };

            db.ChannelRewards.Add(record);
            local.Add(record);
        }

        await db.SaveChangesAsync(cancellationToken);

        var report = RewardBindingResolver.Resolve(RewardCatalog.All, local, remote);

        foreach (var record in local)
        {
            db.ChannelRewards.Update(record);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reward binding sync completed: {Results}",
            string.Join(", ", report.Results.Select(result => $"{result.Code}={result.Status}"))
        );

        return report;
    }
}
