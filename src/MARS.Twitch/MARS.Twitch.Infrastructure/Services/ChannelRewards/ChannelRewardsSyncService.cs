using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
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
/// Проверки полей и сборка update-запроса повторяют отлаженный механизм
/// <c>TemporaryReward.EnsureRewardStateAsync</c> из монолита (минимальный
/// дифф: меняются только изменившиеся поля, чтобы не затирать ручные правки
/// в дашборде Twitch).
/// </summary>
public class ChannelRewardsSyncService(
    ChannelRewardsService channelRewardsService,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    IMarsGrpcClient grpcClient,
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
                    d.IsActive
                ))
                .ToList();
        }
        catch (RpcException)
        {
            definitions = [];
        }

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
                var createReq = BuildCreateRequest(seed, desiredEnabled: seed.IsActive);

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
            var desiredTitle = activeDef?.Title ?? record.Title;
            var desiredPrompt = activeDef?.Prompt ?? record.Prompt;
            var desiredBg = activeDef?.BackgroundColor ?? record.BackgroundColor;
            var desiredInput = activeDef?.IsUserInputRequired ?? record.IsUserInputRequired;

            var match = !string.IsNullOrWhiteSpace(record.TwitchRewardId)
                ? remote.FirstOrDefault(r => r.Id == record.TwitchRewardId)
                : remote.FirstOrDefault(r => r.Cost == cost);

            if (match is null)
            {
                // Награды на Twitch нет — пересоздаём с полной идентичностью.
                var createReq = new CreateCustomRewardsRequest
                {
                    Title = desiredTitle,
                    Cost = cost,
                    IsEnabled = desiredEnabled,
                    Prompt = desiredPrompt,
                    BackgroundColor = desiredBg ?? "#9146FF",
                    IsUserInputRequired = desiredInput,
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
                // Отлаженные проверки из TemporaryReward.EnsureRewardStateAsync:
                // собираем минимальный дифф — только изменившиеся поля.
                var needUpdate = false;
                var updateRequest = new UpdateCustomRewardRequest { IsEnabled = desiredEnabled };

                var desiredGlobalCooldown = record.GlobalCooldownSeconds ?? 0;
                var desiredIsGlobalCooldownEnabled = record.IsGlobalCooldownEnabled;

                if (
                    desiredIsGlobalCooldownEnabled != match.GlobalCooldownSetting.IsEnabled
                    || match.GlobalCooldownSetting.GlobalCooldownSeconds != desiredGlobalCooldown
                )
                {
                    updateRequest.IsGlobalCooldownEnabled = desiredIsGlobalCooldownEnabled;
                    updateRequest.GlobalCooldownSeconds = desiredGlobalCooldown;
                    needUpdate = true;
                }

                if (
                    !string.Equals(
                        match.Title ?? string.Empty,
                        desiredTitle,
                        StringComparison.Ordinal
                    )
                )
                {
                    updateRequest.Title = desiredTitle;
                    needUpdate = true;
                }

                if (
                    !string.Equals(
                        match.Prompt ?? string.Empty,
                        desiredPrompt ?? string.Empty,
                        StringComparison.Ordinal
                    )
                )
                {
                    updateRequest.Prompt = desiredPrompt;
                    needUpdate = true;
                }

                if (match.Cost != cost)
                {
                    updateRequest.Cost = cost;
                    needUpdate = true;
                }

                if (match.IsUserInputRequired != desiredInput)
                {
                    updateRequest.IsUserInputRequired = desiredInput;
                    needUpdate = true;
                }

                if (
                    !string.Equals(
                        match.BackgroundColor ?? string.Empty,
                        (desiredBg ?? string.Empty),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    updateRequest.BackgroundColor = desiredBg ?? "#9146FF";
                    needUpdate = true;
                }

                if (match.MaxPerStreamSetting.IsEnabled != record.IsMaxPerStreamEnabled)
                {
                    updateRequest.IsMaxPerStreamEnabled = record.IsMaxPerStreamEnabled;
                    needUpdate = true;
                }

                if (
                    match.MaxPerUserPerStreamSetting.IsEnabled
                    != record.IsMaxPerUserPerStreamEnabled
                )
                {
                    updateRequest.IsMaxPerUserPerStreamEnabled =
                        record.IsMaxPerUserPerStreamEnabled;
                    needUpdate = true;
                }

                if (match.ShouldRedemptionsSkipQueue != record.ShouldRedemptionsSkipRequestQueue)
                {
                    updateRequest.ShouldRedemptionsSkipRequestQueue =
                        record.ShouldRedemptionsSkipRequestQueue;
                    needUpdate = true;
                }

                if (needUpdate || match.IsEnabled != desiredEnabled)
                {
                    var ok = await channelRewardsService.UpdateRewardAsync(match.Id, updateRequest);

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
                    else
                    {
                        logger.LogError("Не удалось обновить награду канала: {RewardId}", match.Id);
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

    private static CreateCustomRewardsRequest BuildCreateRequest(
        RewardDefinitionSnapshot seed,
        bool desiredEnabled
    ) =>
        new()
        {
            Title = seed.Title,
            Cost = seed.Cost,
            IsEnabled = desiredEnabled,
            Prompt = seed.Prompt,
            BackgroundColor = seed.BackgroundColor,
            IsUserInputRequired = seed.IsUserInputRequired,
        };
}
