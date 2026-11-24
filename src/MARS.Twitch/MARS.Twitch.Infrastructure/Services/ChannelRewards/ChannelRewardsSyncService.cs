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
/// Периодическая синхронизация локальных записей наград с Twitch.
/// Общие стоимости моделируются как «слоты» (как в монолите): одна награда
/// на стоимость, идентичность которой (title/prompt/color) переписывается
/// по активному сегодня хендлеру; IsEnabled = есть хотя бы один активный.
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

        // Группируем определения по стоимости — одна группа = один «слот».
        var groups = definitions.GroupBy(d => d.Cost).ToList();

        // Создание / обновление по слотам.
        foreach (var group in groups)
        {
            var cost = group.Key;
            var activeDefs = group.Where(d => d.IsActive).ToList();

            if (activeDefs.Count > 1)
            {
                logger.LogError(
                    "Конфликт активных определений наград: cost={Cost}, активны [{Names}] — слот не трогаем",
                    cost,
                    string.Join(", ", activeDefs.Select(d => d.Title))
                );
                continue;
            }

            var activeDef = activeDefs.FirstOrDefault();
            var anyActive = activeDef is not null;

            // Идентичность слота: у активного хендлера; иначе сохраняем текущую.
            var record = local.FirstOrDefault(r => r.Cost == cost && !r.IsDeleted);

            if (record is null)
            {
                // Слота в БД нет — создаём из активного определения (или первого доступного).
                var seed = activeDef ?? group.First();
                var createReq = new CreateCustomRewardsRequest
                {
                    Title = seed.Title,
                    Cost = seed.Cost,
                    IsEnabled = seed.IsActive,
                    Prompt = seed.Prompt,
                    BackgroundColor = seed.BackgroundColor,
                    IsUserInputRequired = seed.IsUserInputRequired,
                };

                var id = await channelRewardsService.CreateRewardAsync(createReq);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var newRecord = new ChannelRewardRecord
                    {
                        Title = seed.Title,
                        Cost = seed.Cost,
                        IsEnabled = seed.IsActive,
                        Prompt = seed.Prompt,
                        BackgroundColor = seed.BackgroundColor,
                        IsUserInputRequired = seed.IsUserInputRequired,
                        TwitchRewardId = id,
                    };
                    db.ChannelRewards.Add(newRecord);
                    await db.SaveChangesAsync(cancellationToken);
                    await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
                }

                continue;
            }

            var desiredEnabled = record.IsEnabled && anyActive;

            var match = !string.IsNullOrWhiteSpace(record.TwitchRewardId)
                ? remote.FirstOrDefault(r => r.Id == record.TwitchRewardId)
                : remote.FirstOrDefault(r => r.Cost == cost);

            // Идентичность для сравнения/обновления.
            var desiredTitle = activeDef?.Title ?? record.Title;
            var desiredPrompt = activeDef?.Prompt ?? record.Prompt;
            var desiredBg = activeDef?.BackgroundColor ?? record.BackgroundColor;
            var desiredInput = activeDef?.IsUserInputRequired ?? record.IsUserInputRequired;

            if (match is null)
            {
                var createReq = new CreateCustomRewardsRequest
                {
                    Title = desiredTitle,
                    Cost = cost,
                    IsEnabled = desiredEnabled,
                    Prompt = desiredPrompt,
                    BackgroundColor = desiredBg,
                    IsUserInputRequired = desiredInput,
                };

                var id = await channelRewardsService.CreateRewardAsync(createReq);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    record.TwitchRewardId = id;
                    record.Title = desiredTitle;
                    record.Prompt = desiredPrompt;
                    record.BackgroundColor = desiredBg;
                    record.IsUserInputRequired = desiredInput;
                    record.IsEnabled = desiredEnabled;
                    db.ChannelRewards.Update(record);
                    await db.SaveChangesAsync(cancellationToken);
                    await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
                }
            }
            else
            {
                var needsUpdate =
                    match.Cost != cost
                    || !string.Equals(match.Title, desiredTitle, StringComparison.Ordinal)
                    || match.IsEnabled != desiredEnabled
                    || !string.Equals(
                        match.Prompt ?? string.Empty,
                        desiredPrompt ?? string.Empty,
                        StringComparison.Ordinal
                    )
                    || !string.Equals(
                        match.BackgroundColor ?? string.Empty,
                        desiredBg ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (needsUpdate)
                {
                    var ok = await channelRewardsService.UpdateRewardAsync(
                        match.Id,
                        new UpdateCustomRewardRequest
                        {
                            Title = desiredTitle,
                            Cost = cost,
                            IsEnabled = desiredEnabled,
                            Prompt = desiredPrompt,
                            BackgroundColor = desiredBg,
                            IsUserInputRequired = desiredInput,
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
                        record.Title = desiredTitle;
                        record.Prompt = desiredPrompt;
                        record.BackgroundColor = desiredBg;
                        record.IsUserInputRequired = desiredInput;
                        record.IsEnabled = desiredEnabled;
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

        // Удаление: soft-deleted записи.
        foreach (var record in local.Where(r => r.IsDeleted))
        {
            var match = !string.IsNullOrWhiteSpace(record.TwitchRewardId)
                ? remote.FirstOrDefault(r => r.Id == record.TwitchRewardId)
                : remote.FirstOrDefault(r => r.Cost == record.Cost);

            if (match is not null)
            {
                await channelRewardsService.DeleteRewardAsync(match.Id);
                await channelRewardsService.RewardsCacheService.InvalidateCacheAsync();
            }
        }
    }
}
