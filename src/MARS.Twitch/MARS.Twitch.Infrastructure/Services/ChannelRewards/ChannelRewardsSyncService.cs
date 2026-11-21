using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public class ChannelRewardsSyncService(
    IChannelRewardsService channelRewardsService,
    IRewardBindingSyncService rewardBindingSyncService,
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

    public async Task SyncOnce(CancellationToken cancellationToken)
    {
        try
        {
            await rewardBindingSyncService.SyncAsync(cancellationToken);

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var local = await db.ChannelRewards.AsNoTracking().ToListAsync(cancellationToken);

            IEnumerable<CustomReward>? remoteRaw = null;
            try
            {
                remoteRaw = await channelRewardsService.GetRewardsAsync();
            }
            catch
            {
                remoteRaw = [];
            }

            var remote = (remoteRaw ?? []).ToList();

            // Идентификаторы Twitch, уже привязанные к локальным строкам. Используется,
            // чтобы две локальные записи не претендовали на одну и ту же remote-награду.
            var usedRemoteIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var existing in local.Where(r => !string.IsNullOrWhiteSpace(r.TwitchRewardId)))
            {
                usedRemoteIds.Add(existing.TwitchRewardId!);
            }

            // Создание/обновление
            foreach (var record in local.Where(r => !r.IsDeleted).ToList())
            {
                try
                {
                    await SyncNonDeletedAsync(db, record, remote, usedRemoteIds, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Не удалось синхронизировать награду Id={Id} Title={Title} TwitchRewardId={TwitchRewardId}",
                        record.Id,
                        record.Title,
                        record.TwitchRewardId
                    );
                }
            }

            // Удаление
            foreach (var record in local.Where(r => r.IsDeleted).ToList())
            {
                try
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
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Не удалось удалить награду Id={Id} Title={Title} TwitchRewardId={TwitchRewardId}",
                        record.Id,
                        record.Title,
                        record.TwitchRewardId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }

    private async Task SyncNonDeletedAsync(
        TwitchDbContext db,
        ChannelRewardRecord record,
        List<CustomReward> remote,
        HashSet<string> usedRemoteIds,
        CancellationToken cancellationToken
    )
    {
        var match = !string.IsNullOrWhiteSpace(record.TwitchRewardId)
            ? remote.FirstOrDefault(r => r.Id == record.TwitchRewardId)
            : remote.FirstOrDefault(r =>
                (
                    r.Cost == record.Cost
                    || r.Title.Equals(record.Title, StringComparison.OrdinalIgnoreCase)
                ) && !usedRemoteIds.Contains(r.Id)
            );

        if (match == null)
        {
            // Уже привязанная награда, которой нет в remote-списке: не создаём дубль.
            if (!string.IsNullOrWhiteSpace(record.TwitchRewardId))
            {
                logger.LogInformation(
                    "Награда Id={Id} Title={Title} привязана к {TwitchRewardId}, но в Twitch не найдена; создание пропущено",
                    record.Id,
                    record.Title,
                    record.TwitchRewardId
                );
                return;
            }

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
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            // Если Twitch вернул id, уже привязанный к другой локальной строке — не падаем,
            // а пропускаем привязку текущей строки (существующая строка остаётся как есть).
            var alreadyOwned = await db
                .ChannelRewards.AsNoTracking()
                .AnyAsync(e => e.TwitchRewardId == id && e.Id != record.Id, cancellationToken);
            if (alreadyOwned)
            {
                logger.LogWarning(
                    "CreateRewardAsync вернул id {TwitchRewardId}, уже привязанный к другой строке; привязка для Id={Id} Title={Title} пропущена",
                    id,
                    record.Id,
                    record.Title
                );
                usedRemoteIds.Add(id);
                return;
            }

            var tracked = await db.ChannelRewards.FirstAsync(
                e => e.Id == record.Id,
                cancellationToken
            );

            // Повторная проверка перед записью, чтобы не нарушить уникальный индекс.
            var conflict = await db
                .ChannelRewards.AsNoTracking()
                .AnyAsync(e => e.TwitchRewardId == id && e.Id != record.Id, cancellationToken);
            if (conflict)
            {
                logger.LogWarning(
                    "Конфликт TwitchRewardId {TwitchRewardId} при привязке Id={Id} Title={Title}; операция пропущена",
                    id,
                    record.Id,
                    record.Title
                );
                usedRemoteIds.Add(id);
                return;
            }

            tracked.TwitchRewardId = id;
            db.ChannelRewards.Update(tracked);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                usedRemoteIds.Add(id);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Не удалось сохранить TwitchRewardId {TwitchRewardId} для Id={Id} Title={Title}",
                    id,
                    record.Id,
                    record.Title
                );
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

                if (!ok)
                {
                    // Обновление не удалось — привязку не меняем.
                }
            }

            if (record.TwitchRewardId != match.Id)
            {
                var occupied = await db
                    .ChannelRewards.AsNoTracking()
                    .AnyAsync(
                        e => e.TwitchRewardId == match.Id && e.Id != record.Id,
                        cancellationToken
                    );

                if (occupied)
                {
                    logger.LogWarning(
                        "TwitchRewardId {TwitchRewardId} уже занят другой строкой; привязка для Id={Id} Title={Title} пропущена",
                        match.Id,
                        record.Id,
                        record.Title
                    );
                }
                else
                {
                    var tracked = await db.ChannelRewards.FirstAsync(
                        e => e.Id == record.Id,
                        cancellationToken
                    );

                    tracked.TwitchRewardId = match.Id;
                    db.ChannelRewards.Update(tracked);
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                        usedRemoteIds.Add(match.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Не удалось сохранить TwitchRewardId {TwitchRewardId} для Id={Id} Title={Title}",
                            match.Id,
                            record.Id,
                            record.Title
                        );
                    }
                }
            }
            else
            {
                usedRemoteIds.Add(match.Id);
            }
        }
    }
}
