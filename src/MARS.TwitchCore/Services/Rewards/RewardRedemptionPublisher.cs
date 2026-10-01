using MARS.Shared.Clients;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets;
using SharedTwitchUser = MARS.Shared.Models.TwitchUser;

namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Единственный издатель reward-событий RabbitMQ.
/// </summary>
/// <remarks>
/// В ветке микросервисов издатель был потерян: <c>TwitchMessagesPublisher</c>
/// публиковал только <c>twitch.message.received/deleted</c>, а
/// <c>twitch.reward.redeemed</c> и <c>twitch.reward.&lt;name&gt;</c> не
/// порождались нигде. Из-за этого ~33 обработчика наград в MARS.Alerts
/// (<c>RewardAlertConsumer</c>) и <c>TwitchMediaAlerts</c> получали пустой поток —
/// это блокер №4 из аудита PR.
/// <para>
/// Событие публикуется только из настоящего EventSub
/// <c>channel.channel_points_custom_reward_redemption.add</c> с непустым
/// <c>Reward.Id</c>. Прежний путь через <c>ClientKeyTriggerAlert</c>, который
/// запускал тот же алерт от обычного сообщения в чате (блокер №7), удалён
/// вместе с издателем и не восстанавливался.
/// </para>
/// </remarks>
public class RewardRedemptionPublisher(
    EventSubWebsocketClient eventSub,
    ITwitchEventValidationService validator,
    IMarsEventBus eventBus,
    ITwitchUserEnsureService userEnsureService,
    IMediaStorageClient mediaStorageClient,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    IHostApplicationLifetime lifetime,
    ILogger<RewardRedemptionPublisher> logger
) : IHostedService
{
    private readonly CancellationToken _stoppingToken = lifetime.ApplicationStopping;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStarted.Register(
            () => eventSub.ChannelPointsCustomRewardRedemptionAdd += OnRedemptionAdd
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        eventSub.ChannelPointsCustomRewardRedemptionAdd -= OnRedemptionAdd;

        return Task.CompletedTask;
    }

    private async Task OnRedemptionAdd(object? sender, ChannelPointsCustomRewardRedemptionArgs args)
    {
        try
        {
            var twitchEvent = args.Payload?.Event;

            if (twitchEvent?.Reward is null || string.IsNullOrWhiteSpace(twitchEvent.Reward.Id))
            {
                logger.LogWarning(
                    "Redemption without Reward.Id ignored (user {UserName})",
                    twitchEvent?.UserName
                );
                return;
            }

            var validation = await validator
                .ForRedemption(args)
                .RequireBroadcasterUserId()
                .RequireBroadcasterUserLogin()
                .ValidateWithResponseAsync(twitchEvent.UserName);

            if (validation.IsInvalid)
            {
                return;
            }

            var rewardEvent = await BuildRewardEventAsync(args, _stoppingToken);

            var routingKey = RabbitMqConfig.RewardKey(twitchEvent.Reward.Title);

            await eventBus.PublishAsync(routingKey, rewardEvent, _stoppingToken);

            logger.LogInformation(
                "Reward redeemed published: key={RoutingKey}, reward={RewardTitle}, user={UserName}, media={HasMedia}",
                routingKey,
                twitchEvent.Reward.Title,
                twitchEvent.UserName,
                rewardEvent.Media is not null
            );
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Reward redemption publishing cancelled during shutdown");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish reward redemption event");
        }
    }

    private async Task<RewardRedeemedEvent> BuildRewardEventAsync(
        ChannelPointsCustomRewardRedemptionArgs args,
        CancellationToken cancellationToken
    )
    {
        var twitchEvent = args.Payload!.Event;

        var record = await FindRewardRecordAsync(
            twitchEvent.Reward.Id,
            twitchEvent.Reward.Title,
            cancellationToken
        );

        MediaInfo? media = null;

        if (record?.MediaInfoId is { } mediaInfoId && mediaInfoId != Guid.Empty)
        {
            media = await mediaStorageClient.GetMediaInfoAsync(mediaInfoId, cancellationToken);
        }

        MARS.TwitchCore.Entities.TwitchUser? user = null;

        try
        {
            user = await userEnsureService.EnsureUserExistsAsync(args, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось получить данные пользователя {UserId} для reward-события",
                twitchEvent.UserId
            );
        }

        var result = new RewardRedeemedEvent
        {
            RewardId = twitchEvent.Reward.Id,
            RewardTitle = twitchEvent.Reward.Title,
            Cost = twitchEvent.Reward.Cost,
            UserId = twitchEvent.UserId,
            UserName = twitchEvent.UserName,
            UserInput = twitchEvent.UserInput,
            RedeemedAt = DateTime.UtcNow,
            MessageId = twitchEvent.Id,
            User = ToSharedUser(user, args),
            Media = media,
        };

        return result;
    }

    /// <summary>
    /// Сущность <c>MARS.TwitchCore.Entities.TwitchUser</c> и контрактный
    /// <see cref="TwitchUser"/> в MARS.Shared — разные типы, поэтому конвертируем явно.
    /// </summary>
    private static SharedTwitchUser? ToSharedUser(
        MARS.TwitchCore.Entities.TwitchUser? entity,
        ChannelPointsCustomRewardRedemptionArgs args
    )
    {
        SharedTwitchUser? result = null;

        if (entity is not null)
        {
            result = new SharedTwitchUser
            {
                TwitchId = entity.TwitchId,
                UserLogin = entity.UserLogin,
                DisplayName = entity.DisplayName,
                ProfileImageUrl = entity.ProfileImageUrl,
                ChatColor = entity.ChatColor,
                IsModerator = entity.IsModerator,
                IsVip = entity.IsVip,
            };
        }
        else if (!string.IsNullOrWhiteSpace(args.Payload?.Event?.UserId))
        {
            result = new SharedTwitchUser
            {
                TwitchId = args.Payload.Event.UserId,
                UserLogin = args.Payload.Event.UserName,
                DisplayName = args.Payload.Event.UserName,
            };
        }

        return result;
    }

    private async Task<ChannelRewardRecord?> FindRewardRecordAsync(
        string twitchRewardId,
        string rewardTitle,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var byTwitchId = await dbContext
            .ChannelRewards.AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.TwitchRewardId == twitchRewardId,
                cancellationToken
            );

        if (byTwitchId is not null)
        {
            return byTwitchId;
        }

        return await dbContext
            .ChannelRewards.AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.Title == rewardTitle && !record.IsDeleted,
                cancellationToken
            );
    }
}
