using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Alerts;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

/// <summary>
/// Периодическая синхронизация локальных записей наград с Twitch:
/// создаёт недостающие, обновляет изменившиеся, удаляет помеченные,
/// применяет планировочную активность из MARS.Alerts.
/// </summary>
public class ChannelRewardsSyncService(
    ChannelRewardsService channelRewardsService,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    IRewardDefinitionsClient rewardDefinitionsClient,
    IOptions<TwitchRewardsOptions> options,
    ILogger<ChannelRewardsSyncService> logger
) : BackgroundService
{
    public async Task SyncNow(CancellationToken cancellationToken = default) =>
        await SyncOnce(cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.SyncIntervalMinutes));
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

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncOnce(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var local = await db.ChannelRewards.ToListAsync(cancellationToken);
        var remote = (await channelRewardsService.GetRewardsAsync())?.ToList() ?? [];

        var definitions = await rewardDefinitionsClient.GetDefinitionsAsync(cancellationToken);
        var schedulingByCost = definitions.ToDictionary(d => d.Cost, d => d.IsActive);

        // Создание / обновление
        foreach (var record in local.Where(r => !r.IsDeleted))
        {
            var schedulingActive = schedulingByCost.TryGetValue(record.Cost, out var sa)
                ? sa
                : true;

            var desiredEnabled = record.IsEnabled && schedulingActive;

            var match = !string.IsNullOrWhiteSpace(record.TwitchRewardId)
                ? remote.FirstOrDefault(r => r.Id == record.TwitchRewardId)
                : remote.FirstOrDefault(r =>
                    r.Cost == record.Cost
                    || string.Equals(r.Title, record.Title, StringComparison.OrdinalIgnoreCase)
                );

            if (match is null)
            {
                var createReq = new CreateCustomRewardsRequest
                {
                    Title = record.Title,
                    Cost = record.Cost,
                    IsEnabled = desiredEnabled,
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
                    record.TwitchRewardId = id;
                    db.ChannelRewards.Update(record);
                    await db.SaveChangesAsync(cancellationToken);
                    await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
                }
            }
            else
            {
                var needsUpdate =
                    match.Cost != record.Cost
                    || !string.Equals(match.Title, record.Title, StringComparison.Ordinal)
                    || match.IsEnabled != desiredEnabled
                    || !string.Equals(
                        match.Prompt ?? string.Empty,
                        record.Prompt ?? string.Empty,
                        StringComparison.Ordinal
                    )
                    || !string.Equals(
                        match.BackgroundColor ?? string.Empty,
                        record.BackgroundColor ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (needsUpdate)
                {
                    var ok = await channelRewardsService.UpdateRewardAsync(
                        match.Id,
                        new UpdateCustomRewardRequest
                        {
                            Title = record.Title,
                            Cost = record.Cost,
                            IsEnabled = desiredEnabled,
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

                    if (ok)
                    {
                        record.TwitchRewardId = match.Id;
                        db.ChannelRewards.Update(record);
                        await db.SaveChangesAsync(cancellationToken);
                        await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
                    }
                }
                else if (record.TwitchRewardId != match.Id)
                {
                    record.TwitchRewardId = match.Id;
                    db.ChannelRewards.Update(record);
                    await db.SaveChangesAsync(cancellationToken);
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
                    || string.Equals(r.Title, record.Title, StringComparison.OrdinalIgnoreCase)
                );

            if (match is not null)
            {
                await channelRewardsService.DeleteRewardAsync(match.Id);
                await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
            }
        }
    }
}
