using System.Globalization;
using System.Text;
using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Alerts;
using MARS.Twitch.Application.Models;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public class ChannelRewardsSyncService(
    ChannelRewardsService channelRewardsService,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    IMarsGrpcClient grpcClient,
    IOptions<TwitchRewardsOptions> options,
    ITelegramRewardNotifier? telegramNotifier,
    ILogger<ChannelRewardsSyncService> logger
) : BackgroundService
{
    private readonly SemaphoreSlim _syncSemaphore = new(1, 1);
    private bool _hasAppliedReconcile;

    public async Task<RewardSyncResult> SyncNow(
        CancellationToken cancellationToken = default,
        IReadOnlySet<string>? rewardCodes = null,
        bool fullReconcile = true
    )
    {
        await _syncSemaphore.WaitAsync(cancellationToken);
        try
        {
            return await SyncOnce(cancellationToken, rewardCodes, fullReconcile);
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    public Task<RewardSyncResult> SyncRewardAsync(
        string rewardCode,
        CancellationToken cancellationToken = default
    ) =>
        SyncNow(
            cancellationToken,
            new HashSet<string>(StringComparer.Ordinal) { rewardCode },
            false
        );

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.SyncIntervalMinutes));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncNow(stoppingToken);
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

    private async Task<RewardSyncResult> SyncOnce(
        CancellationToken cancellationToken,
        IReadOnlySet<string>? rewardCodes = null,
        bool fullReconcile = true
    )
    {
        var result = new RewardSyncResult { DryRun = !_hasAppliedReconcile };

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var local = await db.ChannelRewards.ToListAsync(cancellationToken);

        var remoteResult = await channelRewardsService.GetRewardsAsync();
        if (remoteResult is null)
        {
            result.Errors.Add("Не удалось получить список наград Twitch");
            await NotifyIssuesAsync(result, cancellationToken);
            return result;
        }

        var remote = remoteResult.ToList();

        List<RewardDefinitionSnapshot> definitions;
        try
        {
            var response = await grpcClient.Alerts.GetRewardDefinitionsAsync(cancellationToken);
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
            result.Errors.Add("Не удалось получить определения наград MARS.Alerts");
            await NotifyIssuesAsync(result, cancellationToken);
            return result;
        }

        if (rewardCodes is not null && rewardCodes.Count > 0)
        {
            definitions = definitions.Where(d => rewardCodes.Contains(d.RewardCode)).ToList();
        }

        var dryRun = result.DryRun;
        var protectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var desiredRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toDelete = new List<CustomReward>();
        var toUpdate =
            new List<(
                ChannelRewardRecord Record,
                CustomReward Remote,
                RewardDefinitionSnapshot Definition
            )>();
        var toCreate =
            new List<(ChannelRewardRecord Record, RewardDefinitionSnapshot Definition)>();

        // Backfill and merge definitions into local records.
        foreach (var def in definitions)
        {
            if (string.IsNullOrWhiteSpace(def.RewardCode))
            {
                result.Errors.Add($"Определение \"{def.Title}\" не содержит RewardCode");
                result.Skipped++;
                continue;
            }

            if (!ValidateDefinition(def, result))
            {
                result.Skipped++;
                continue;
            }

            var record = FindOrCreateLocalRecord(db, local, def);
            if (record is null)
            {
                result.Errors.Add($"Не удалось подготовить локальную запись для {def.RewardCode}");
                result.Skipped++;
                continue;
            }

            MergeSettings(record, def);

            if (!def.IsActive || !record.IsEnabled)
            {
                if (!string.IsNullOrWhiteSpace(record.TwitchRewardId))
                {
                    var remoteMatch = remote.FirstOrDefault(r =>
                        string.Equals(
                            r.Id,
                            record.TwitchRewardId,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                    if (remoteMatch is not null)
                    {
                        toDelete.Add(remoteMatch);
                    }
                }

                continue;
            }

            var candidates = remote
                .Where(r =>
                    string.Equals(
                        NormalizeTitle(r.Title),
                        NormalizeTitle(def.Title),
                        StringComparison.Ordinal
                    )
                    && r.Cost == def.Cost
                )
                .ToList();

            CustomReward? match = null;
            if (!string.IsNullOrWhiteSpace(record.TwitchRewardId))
            {
                match = remote.FirstOrDefault(r =>
                    string.Equals(r.Id, record.TwitchRewardId, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (match is null)
            {
                if (candidates.Count > 1)
                {
                    result.Ambiguities++;
                    foreach (var candidate in candidates)
                    {
                        protectedIds.Add(candidate.Id);
                    }

                    var ambiguityMessage =
                        $"Неоднозначное сопоставление награды {def.RewardCode} ({def.Title}, cost {def.Cost}): "
                        + string.Join(", ", candidates.Select(c => $"{c.Title} [{c.Id}]"));
                    await NotifyIssueAsync(ambiguityMessage, cancellationToken);
                    result.Skipped++;
                    continue;
                }

                if (candidates.Count == 1)
                {
                    match = candidates[0];
                }
            }
            else if (
                candidates.Count > 0
                && !candidates.Any(c =>
                    string.Equals(c.Id, match.Id, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                // Stored ID points to a reward with another title/cost; treat as ambiguous.
                result.Ambiguities++;
                foreach (var candidate in candidates)
                {
                    protectedIds.Add(candidate.Id);
                }

                protectedIds.Add(match.Id);
                var ambiguityMessage =
                    $"Неоднозначное сопоставление награды {def.RewardCode}: "
                    + $"сохранённый ID {match.Id} не совпадает с кандидатами "
                    + string.Join(", ", candidates.Select(c => $"{c.Title} [{c.Id}]"));
                await NotifyIssueAsync(ambiguityMessage, cancellationToken);
                result.Skipped++;
                continue;
            }

            if (match is null)
            {
                toCreate.Add((record, def));
            }
            else
            {
                desiredRemoteIds.Add(match.Id);
                toUpdate.Add((record, match, def));
            }
        }

        if (fullReconcile)
        {
            foreach (var reward in remote)
            {
                if (!desiredRemoteIds.Contains(reward.Id) && !protectedIds.Contains(reward.Id))
                {
                    toDelete.Add(reward);
                }
            }
        }

        toDelete = toDelete.DistinctBy(r => r.Id, StringComparer.OrdinalIgnoreCase).ToList();

        if (dryRun)
        {
            logger.LogInformation(
                "Dry-run синхронизации наград: create={Create}, update={Update}, delete={Delete}, skipped={Skipped}, ambiguities={Ambiguities}",
                toCreate.Count,
                toUpdate.Count,
                toDelete.Count,
                result.Skipped,
                result.Ambiguities
            );

            _hasAppliedReconcile = true;
            await NotifyIssuesAsync(result, cancellationToken);
            return result;
        }

        // Delete first to free capacity.
        foreach (var reward in toDelete)
        {
            var deleted = await channelRewardsService.DeleteRewardAsync(reward.Id);
            if (deleted)
            {
                result.Deleted++;
                await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
            }
            else
            {
                result.Errors.Add(
                    $"Не удалось удалить награду Twitch: {reward.Title} [{reward.Id}]"
                );
            }
        }

        foreach (var (record, remoteReward, def) in toUpdate)
        {
            var updateRequest = BuildUpdateRequest(record, remoteReward, def);
            var updated = await channelRewardsService.UpdateRewardAsync(
                remoteReward.Id,
                updateRequest
            );
            if (updated)
            {
                result.Updated++;
                record.TwitchRewardId = remoteReward.Id;
                record.Title = def.Title;
                record.Prompt = def.Prompt;
                record.BackgroundColor = def.BackgroundColor;
                record.IsUserInputRequired = def.IsUserInputRequired;
                record.IsEnabled = record.IsEnabled && def.IsActive;
                db.ChannelRewards.Update(record);
                await db.SaveChangesAsync(cancellationToken);
                await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
            }
            else
            {
                result.Errors.Add(
                    $"Не удалось обновить награду Twitch: {def.Title} [{remoteReward.Id}]"
                );
            }
        }

        foreach (var (record, def) in toCreate)
        {
            var createRequest = BuildCreateRequest(record, def);
            var id = await channelRewardsService.CreateRewardAsync(createRequest);
            if (!string.IsNullOrWhiteSpace(id))
            {
                result.Created++;
                record.TwitchRewardId = id;
                record.Title = def.Title;
                record.Prompt = def.Prompt;
                record.BackgroundColor = def.BackgroundColor;
                record.IsUserInputRequired = def.IsUserInputRequired;
                record.IsEnabled = record.IsEnabled && def.IsActive;
                record.BindingStatus = "Bound";
                db.ChannelRewards.Update(record);
                await db.SaveChangesAsync(cancellationToken);
                await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
            }
            else
            {
                result.Errors.Add($"Не удалось создать награду Twitch: {def.Title}");
            }
        }

        _hasAppliedReconcile = true;
        await NotifyIssuesAsync(result, cancellationToken);
        return result;
    }

    private ChannelRewardRecord? FindOrCreateLocalRecord(
        TwitchDbContext db,
        List<ChannelRewardRecord> local,
        RewardDefinitionSnapshot def
    )
    {
        var record = local.FirstOrDefault(r =>
            !r.IsDeleted && string.Equals(r.RewardCode, def.RewardCode, StringComparison.Ordinal)
        );

        if (record is not null)
        {
            return record;
        }

        var legacyCandidates = local
            .Where(r =>
                string.IsNullOrWhiteSpace(r.RewardCode)
                && string.Equals(
                    NormalizeTitle(r.Title),
                    NormalizeTitle(def.Title),
                    StringComparison.Ordinal
                )
                && r.Cost == def.Cost
            )
            .ToList();

        if (legacyCandidates.Count == 1)
        {
            var legacy = legacyCandidates[0];
            legacy.RewardCode = def.RewardCode;
            legacy.BindingStatus = "NeedsBackfill";
            db.ChannelRewards.Update(legacy);
            return legacy;
        }

        var newRecord = new ChannelRewardRecord
        {
            Title = def.Title,
            Cost = def.Cost,
            IsEnabled = true,
            Prompt = def.Prompt,
            BackgroundColor = def.BackgroundColor,
            IsUserInputRequired = def.IsUserInputRequired,
            RewardCode = def.RewardCode,
            BindingStatus = "Unknown",
            IsDeleted = false,
        };

        db.ChannelRewards.Add(newRecord);
        local.Add(newRecord);
        return newRecord;
    }

    private static void MergeSettings(ChannelRewardRecord record, RewardDefinitionSnapshot def)
    {
        if (def.IsMaxPerStreamEnabled.HasValue)
        {
            record.IsMaxPerStreamEnabled = def.IsMaxPerStreamEnabled.Value;
        }

        if (def.MaxPerStream.HasValue)
        {
            record.MaxPerStream = def.MaxPerStream;
        }

        if (def.IsMaxPerUserPerStreamEnabled.HasValue)
        {
            record.IsMaxPerUserPerStreamEnabled = def.IsMaxPerUserPerStreamEnabled.Value;
        }

        if (def.MaxPerUserPerStream.HasValue)
        {
            record.MaxPerUserPerStream = def.MaxPerUserPerStream;
        }

        if (def.IsGlobalCooldownEnabled.HasValue)
        {
            record.IsGlobalCooldownEnabled = def.IsGlobalCooldownEnabled.Value;
        }

        if (def.GlobalCooldownSeconds.HasValue)
        {
            record.GlobalCooldownSeconds = def.GlobalCooldownSeconds;
        }

        if (def.ShouldRedemptionsSkipRequestQueue.HasValue)
        {
            record.ShouldRedemptionsSkipRequestQueue = def.ShouldRedemptionsSkipRequestQueue.Value;
        }
    }

    private bool ValidateDefinition(RewardDefinitionSnapshot def, RewardSyncResult result)
    {
        if (
            def.IsMaxPerStreamEnabled == true
            && (!def.MaxPerStream.HasValue || def.MaxPerStream <= 0)
        )
        {
            result.Errors.Add($"Некорректный лимит на стрим для {def.RewardCode}");
            return false;
        }

        if (
            def.IsMaxPerUserPerStreamEnabled == true
            && (!def.MaxPerUserPerStream.HasValue || def.MaxPerUserPerStream <= 0)
        )
        {
            result.Errors.Add($"Некорректный лимит на пользователя для {def.RewardCode}");
            return false;
        }

        if (
            def.IsGlobalCooldownEnabled == true
            && (!def.GlobalCooldownSeconds.HasValue || def.GlobalCooldownSeconds < 0)
        )
        {
            result.Errors.Add($"Некорректный cooldown для {def.RewardCode}");
            return false;
        }

        return true;
    }

    private static CreateCustomRewardsRequest BuildCreateRequest(
        ChannelRewardRecord record,
        RewardDefinitionSnapshot def
    )
    {
        return new CreateCustomRewardsRequest
        {
            Title = record.Title,
            Cost = record.Cost,
            IsEnabled = record.IsEnabled && def.IsActive,
            Prompt = record.Prompt,
            BackgroundColor = record.BackgroundColor ?? "#9146FF",
            IsUserInputRequired = record.IsUserInputRequired,
            IsMaxPerStreamEnabled = record.IsMaxPerStreamEnabled,
            MaxPerStream = record.MaxPerStream,
            IsMaxPerUserPerStreamEnabled = record.IsMaxPerUserPerStreamEnabled,
            MaxPerUserPerStream = record.MaxPerUserPerStream,
            IsGlobalCooldownEnabled = record.IsGlobalCooldownEnabled,
            GlobalCooldownSeconds = record.GlobalCooldownSeconds ?? 0,
            ShouldRedemptionsSkipRequestQueue = record.ShouldRedemptionsSkipRequestQueue,
        };
    }

    private static UpdateCustomRewardRequest BuildUpdateRequest(
        ChannelRewardRecord record,
        CustomReward remote,
        RewardDefinitionSnapshot def
    )
    {
        var request = new UpdateCustomRewardRequest
        {
            IsEnabled = record.IsEnabled && def.IsActive,
        };

        if (remote.Title != record.Title)
        {
            request.Title = record.Title;
        }

        if (remote.Prompt != record.Prompt)
        {
            request.Prompt = record.Prompt;
        }

        if (remote.Cost != record.Cost)
        {
            request.Cost = record.Cost;
        }

        if (remote.IsUserInputRequired != record.IsUserInputRequired)
        {
            request.IsUserInputRequired = record.IsUserInputRequired;
        }

        if (
            !string.Equals(
                remote.BackgroundColor,
                record.BackgroundColor,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            request.BackgroundColor = record.BackgroundColor ?? "#9146FF";
        }

        if (
            remote.GlobalCooldownSetting.IsEnabled != record.IsGlobalCooldownEnabled
            || remote.GlobalCooldownSetting.GlobalCooldownSeconds
                != (record.GlobalCooldownSeconds ?? 0)
        )
        {
            request.IsGlobalCooldownEnabled = record.IsGlobalCooldownEnabled;
            request.GlobalCooldownSeconds = record.GlobalCooldownSeconds ?? 0;
        }

        if (remote.MaxPerStreamSetting.IsEnabled != record.IsMaxPerStreamEnabled)
        {
            request.IsMaxPerStreamEnabled = record.IsMaxPerStreamEnabled;
        }

        if (remote.MaxPerStreamSetting.MaxPerStream != record.MaxPerStream)
        {
            request.MaxPerStream = record.MaxPerStream;
        }

        if (remote.MaxPerUserPerStreamSetting.IsEnabled != record.IsMaxPerUserPerStreamEnabled)
        {
            request.IsMaxPerUserPerStreamEnabled = record.IsMaxPerUserPerStreamEnabled;
        }

        if (remote.MaxPerUserPerStreamSetting.MaxPerUserPerStream != record.MaxPerUserPerStream)
        {
            request.MaxPerUserPerStream = record.MaxPerUserPerStream;
        }

        if (remote.ShouldRedemptionsSkipQueue != record.ShouldRedemptionsSkipRequestQueue)
        {
            request.ShouldRedemptionsSkipRequestQueue = record.ShouldRedemptionsSkipRequestQueue;
        }

        if (remote.IsEnabled != (record.IsEnabled && def.IsActive)) { }

        return request;
    }

    private async Task NotifyIssueAsync(string message, CancellationToken cancellationToken)
    {
        if (telegramNotifier is null)
        {
            logger.LogWarning("{Message}", message);
            return;
        }

        await telegramNotifier.NotifyRewardSyncIssueAsync(message, cancellationToken);
    }

    private async Task NotifyIssuesAsync(
        RewardSyncResult result,
        CancellationToken cancellationToken
    )
    {
        if (result.Ambiguities > 0 || result.Errors.Count > 0)
        {
            var builder = new StringBuilder();
            if (result.Ambiguities > 0)
            {
                builder.AppendLine($"Неоднозначностей: {result.Ambiguities}");
            }

            foreach (var error in result.Errors)
            {
                builder.AppendLine(error);
            }

            await NotifyIssueAsync(builder.ToString().Trim(), cancellationToken);
        }
    }

    private static string NormalizeTitle(string? title) =>
        (title ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC).Trim();

    private static string NormalizeId(string? id) => (id ?? string.Empty).Trim();
}
