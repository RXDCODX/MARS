using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Alerts;
using MARS.Twitch.Application.Models;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public class ChannelRewardsManager(
    ChannelRewardsService channelRewardsService,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ILogger<ChannelRewardsManager> logger,
    IServiceProvider serviceProvider,
    IMarsGrpcClient? grpcClient = null
)
{
    public async Task<ChannelRewardRecord?> UpsertLocalAsync(ChannelRewardDefinition definition)
    {
        ChannelRewardRecord? result = null;

        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var existing = !string.IsNullOrWhiteSpace(definition.RewardCode)
                ? await db.ChannelRewards.FirstOrDefaultAsync(r =>
                    r.RewardCode == definition.RewardCode
                )
                : await db.ChannelRewards.FirstOrDefaultAsync(r =>
                    r.Cost == definition.Cost || r.Title == definition.Title
                );

            if (existing == null)
            {
                var rec = new ChannelRewardRecord
                {
                    Title = definition.Title,
                    Cost = definition.Cost,
                    IsEnabled = true,
                    Prompt = definition.Prompt,
                    BackgroundColor = definition.BackgroundColor,
                    IsUserInputRequired = definition.IsUserInputRequired,
                    RewardCode = definition.RewardCode,
                    IsMaxPerStreamEnabled = definition.IsMaxPerStreamEnabled ?? false,
                    MaxPerStream = definition.MaxPerStream,
                    IsMaxPerUserPerStreamEnabled = definition.IsMaxPerUserPerStreamEnabled ?? false,
                    MaxPerUserPerStream = definition.MaxPerUserPerStream,
                    IsGlobalCooldownEnabled = definition.IsGlobalCooldownEnabled ?? false,
                    GlobalCooldownSeconds = definition.GlobalCooldownSeconds,
                    ShouldRedemptionsSkipRequestQueue =
                        definition.ShouldRedemptionsSkipRequestQueue ?? false,
                    IsDeleted = false,
                    MediaInfoId = (definition as PyroAlertRewardDefinition)?.MediaInfoId,
                };
                db.Add(rec);
                await db.SaveChangesAsync();
                result = rec;
            }
            else
            {
                existing.Title = definition.Title;
                existing.Cost = definition.Cost;
                existing.Prompt = definition.Prompt;
                existing.BackgroundColor = definition.BackgroundColor;
                existing.IsUserInputRequired = definition.IsUserInputRequired;
                existing.RewardCode = definition.RewardCode ?? existing.RewardCode;

                if (definition.IsMaxPerStreamEnabled.HasValue)
                {
                    existing.IsMaxPerStreamEnabled = definition.IsMaxPerStreamEnabled.Value;
                }

                if (definition.MaxPerStream.HasValue)
                {
                    existing.MaxPerStream = definition.MaxPerStream;
                }

                if (definition.IsMaxPerUserPerStreamEnabled.HasValue)
                {
                    existing.IsMaxPerUserPerStreamEnabled = definition
                        .IsMaxPerUserPerStreamEnabled
                        .Value;
                }

                if (definition.MaxPerUserPerStream.HasValue)
                {
                    existing.MaxPerUserPerStream = definition.MaxPerUserPerStream;
                }

                if (definition.IsGlobalCooldownEnabled.HasValue)
                {
                    existing.IsGlobalCooldownEnabled = definition.IsGlobalCooldownEnabled.Value;
                }

                if (definition.GlobalCooldownSeconds.HasValue)
                {
                    existing.GlobalCooldownSeconds = definition.GlobalCooldownSeconds;
                }

                if (definition.ShouldRedemptionsSkipRequestQueue.HasValue)
                {
                    existing.ShouldRedemptionsSkipRequestQueue = definition
                        .ShouldRedemptionsSkipRequestQueue
                        .Value;
                }

                existing.IsDeleted = false;
                existing.MediaInfoId = (definition as PyroAlertRewardDefinition)?.MediaInfoId;
                db.Update(existing);
                await db.SaveChangesAsync();
                result = existing;
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }

        return result;
    }

    public async Task<IEnumerable<CustomReward>?> GetAllAsync()
    {
        return await channelRewardsService.GetRewardsAsync();
    }

    public async Task<List<ChannelRewardRecord>> GetLocalAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var list = await db.ChannelRewards.AsNoTracking().OrderBy(r => r.Title).ToListAsync();
        return list;
    }

    public async Task<ChannelRewardRecord?> GetLocalByIdAsync(Guid localId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var rec = await db.ChannelRewards.AsNoTracking().FirstOrDefaultAsync(r => r.Id == localId);
        return rec;
    }

    public async Task<CustomReward?> GetByIdAsync(string rewardId)
    {
        return await channelRewardsService.GetRewardByIdAsync(rewardId);
    }

    public async Task<bool> SoftDeleteLocalAsync(Guid localId)
    {
        var result = false;
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var rec = await db.ChannelRewards.FirstOrDefaultAsync(e => e.Id == localId);
            if (rec != null)
            {
                rec.IsDeleted = true;
                db.Update(rec);
                await db.SaveChangesAsync();
                result = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }

        return result;
    }

    public async Task<bool> UpdateLocalAsync(Guid localId, UpdateCustomRewardDto dto)
    {
        var result = false;
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var rec = await db.ChannelRewards.FirstOrDefaultAsync(e => e.Id == localId);
            if (rec != null)
            {
                // Метаданные определения управляются MARS.Alerts и не меняются из UI.
                rec.IsEnabled = dto.IsEnabled ?? rec.IsEnabled;
                rec.IsMaxPerStreamEnabled = dto.IsMaxPerStreamEnabled ?? rec.IsMaxPerStreamEnabled;
                rec.MaxPerStream = dto.MaxPerStream ?? rec.MaxPerStream;
                rec.IsMaxPerUserPerStreamEnabled =
                    dto.IsMaxPerUserPerStreamEnabled ?? rec.IsMaxPerUserPerStreamEnabled;
                rec.MaxPerUserPerStream = dto.MaxPerUserPerStream ?? rec.MaxPerUserPerStream;
                rec.IsGlobalCooldownEnabled =
                    dto.IsGlobalCooldownEnabled ?? rec.IsGlobalCooldownEnabled;
                rec.GlobalCooldownSeconds = dto.GlobalCooldownSeconds ?? rec.GlobalCooldownSeconds;
                rec.ShouldRedemptionsSkipRequestQueue =
                    dto.ShouldRedemptionsSkipRequestQueue ?? rec.ShouldRedemptionsSkipRequestQueue;

                db.Update(rec);
                await db.SaveChangesAsync();
                result = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }

        return result;
    }

    /// <summary>
    /// Импортирует определения наград из MARS.Alerts в локальную БД.
    /// </summary>
    public async Task<int> SyncRewardDefinitionsToLocalAsync()
    {
        if (grpcClient is null)
        {
            return 0;
        }

        List<RewardDefinitionSnapshot> definitions;
        try
        {
            var response = await grpcClient.Alerts.GetRewardDefinitionsAsync();
            definitions = response
                .Definitions.Select(d => new RewardDefinitionSnapshot(
                    d.Cost,
                    d.Title,
                    d.Prompt,
                    d.BackgroundColor,
                    d.IsUserInputRequired,
                    d.IsActive,
                    string.IsNullOrWhiteSpace(d.RewardCode) ? string.Empty : d.RewardCode,
                    d.HasIsMaxPerStreamEnabled ? d.IsMaxPerStreamEnabled : null,
                    d.HasMaxPerStream ? d.MaxPerStream : null,
                    d.HasIsMaxPerUserPerStreamEnabled ? d.IsMaxPerUserPerStreamEnabled : null,
                    d.HasMaxPerUserPerStream ? d.MaxPerUserPerStream : null,
                    d.HasIsGlobalCooldownEnabled ? d.IsGlobalCooldownEnabled : null,
                    d.HasGlobalCooldownSeconds ? d.GlobalCooldownSeconds : null,
                    d.HasShouldRedemptionsSkipRequestQueue
                        ? d.ShouldRedemptionsSkipRequestQueue
                        : null
                ))
                .ToList();
        }
        catch (RpcException)
        {
            return 0;
        }
        var count = 0;
        foreach (var def in definitions)
        {
            var record = await UpsertLocalAsync(
                new ChannelRewardDefinition
                {
                    Title = def.Title,
                    Cost = def.Cost,
                    IsEnabled = true,
                    Prompt = def.Prompt,
                    BackgroundColor = def.BackgroundColor,
                    IsUserInputRequired = def.IsUserInputRequired,
                    RewardCode = def.RewardCode,
                    IsMaxPerStreamEnabled = def.IsMaxPerStreamEnabled,
                    MaxPerStream = def.MaxPerStream,
                    IsMaxPerUserPerStreamEnabled = def.IsMaxPerUserPerStreamEnabled,
                    MaxPerUserPerStream = def.MaxPerUserPerStream,
                    IsGlobalCooldownEnabled = def.IsGlobalCooldownEnabled,
                    GlobalCooldownSeconds = def.GlobalCooldownSeconds,
                    ShouldRedemptionsSkipRequestQueue = def.ShouldRedemptionsSkipRequestQueue,
                }
            );
            if (record is not null)
            {
                count++;
            }
        }

        return count;
    }
}
