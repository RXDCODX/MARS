using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.TwitchCore.Services.ChannelRewards;

public class ChannelRewardsSyncService(
    ChannelRewardsService channelRewardsService,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ILogger<ChannelRewardsSyncService> logger
) : BackgroundService
{
    public async Task SyncNow(CancellationToken cancellationToken = default)
    {
        await SyncOnce(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task SyncOnce(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var local = await db.ChannelRewards.AsNoTracking().ToListAsync(cancellationToken);
        var remote = await channelRewardsService.GetRewardsAsync() ?? [];

        // Создание/обновление
        foreach (var record in local.Where(r => !r.IsDeleted))
        {
            var match = !string.IsNullOrWhiteSpace(record.TwitchRewardId)
                ? remote.FirstOrDefault(r => r.Id == record.TwitchRewardId)
                : remote.FirstOrDefault(r =>
                    r.Cost == record.Cost
                    || r.Title.Equals(record.Title, StringComparison.OrdinalIgnoreCase)
                );

            if (match == null)
            {
                var createReq = new CreateCustomRewardsRequest
                {
                    Title = record.Title,
                    Cost = record.Cost,
                    IsEnabled = record.IsEnabled,
                    Prompt = record.Prompt,
                    BackgroundColor = record.BackgroundColor,
                    IsUserInputRequired = record.IsUserInputRequired,
                    IsMaxPerStreamEnabled = record.IsMaxPerStreamEnabled,
                    MaxPerStream = record.MaxPerStream,
                    IsMaxPerUserPerStreamEnabled = record.IsMaxPerUserPerStreamEnabled,
                    MaxPerUserPerStream = record.MaxPerUserPerStream,
                    IsGlobalCooldownEnabled = record.IsGlobalCooldownEnabled,
                    GlobalCooldownSeconds = record.GlobalCooldownSeconds,
                    ShouldRedemptionsSkipRequestQueue = record.ShouldRedemptionsSkipRequestQueue,
                };

                var id = await channelRewardsService.CreateRewardAsync(createReq);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var tracked = await db.ChannelRewards.FirstAsync(
                        e => e.Id == record.Id,
                        cancellationToken
                    );
                    tracked.TwitchRewardId = id;
                    db.ChannelRewards.Update(tracked);
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                var needsUpdate =
                    match.Cost != record.Cost
                    || !string.Equals(match.Title, record.Title, StringComparison.Ordinal);
                if (needsUpdate)
                {
                    var ok = await channelRewardsService.UpdateRewardAsync(
                        match.Id,
                        new UpdateCustomRewardRequest
                        {
                            Title = record.Title,
                            Cost = record.Cost,
                            IsEnabled = record.IsEnabled,
                            Prompt = record.Prompt,
                            BackgroundColor = record.BackgroundColor,
                            IsUserInputRequired = record.IsUserInputRequired,
                            IsMaxPerStreamEnabled = record.IsMaxPerStreamEnabled,
                            MaxPerStream = record.MaxPerStream,
                            IsMaxPerUserPerStreamEnabled = record.IsMaxPerUserPerStreamEnabled,
                            MaxPerUserPerStream = record.MaxPerUserPerStream,
                            IsGlobalCooldownEnabled = record.IsGlobalCooldownEnabled,
                            GlobalCooldownSeconds = record.GlobalCooldownSeconds,
                            ShouldRedemptionsSkipRequestQueue =
                                record.ShouldRedemptionsSkipRequestQueue,
                        }
                    );

                    if (ok && record.TwitchRewardId != match.Id)
                    {
                        var tracked = await db.ChannelRewards.FirstAsync(
                            e => e.Id == record.Id,
                            cancellationToken
                        );
                        tracked.TwitchRewardId = match.Id;
                        db.ChannelRewards.Update(tracked);
                        await db.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }

        // Удаление
        foreach (var record in local.Where(r => r.IsDeleted))
        {
            var match = !string.IsNullOrWhiteSpace(record.TwitchRewardId)
                ? remote.FirstOrDefault(r => r.Id == record.TwitchRewardId)
                : remote.FirstOrDefault(r =>
                    r.Cost == record.Cost
                    || r.Title.Equals(record.Title, StringComparison.OrdinalIgnoreCase)
                );

            if (match != null)
            {
                await channelRewardsService.DeleteRewardAsync(match.Id);
            }
        }
    }
}
