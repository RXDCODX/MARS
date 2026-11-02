using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch;

/// <summary>
/// Converts selected Twitch EventSub notifications into the overlay's single
/// TwitchEventAlert SignalR event. Reward redemptions intentionally stay on the
/// existing twitch.reward.* pipeline so a redemption is never shown twice.
/// </summary>
public sealed class TwitchEventSubAlertDispatcher(
    TwitchEventSubConsumer eventSub,
    ITelegramusNotifier notifier,
    ILogger<TwitchEventSubAlertDispatcher> logger
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        eventSub.EventReceived += OnEventReceivedAsync;
        logger.LogInformation("Twitch EventSub alert bridge subscribed");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        eventSub.EventReceived -= OnEventReceivedAsync;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Public dispatch entry point keeps the mapping independently testable.
    /// </summary>
    public Task DispatchAsync(TwitchEventSubNotification notification)
    {
        return notification.EventData switch
        {
            TwitchChannelFollowEvent follow => PublishAsync(
                notification,
                "Новый подписчик",
                $"{DisplayName(follow.UserName, follow.UserLogin)} подписался на канал",
                follow.UserId,
                follow.UserLogin,
                follow.UserName,
                0,
                follow.FollowedAt
            ),
            TwitchChannelRaidEvent raid => PublishAsync(
                notification,
                "РЕЙД",
                $"{DisplayName(raid.FromBroadcasterUserName, raid.FromBroadcasterUserLogin)} привёл {raid.Viewers} зрителей",
                raid.FromBroadcasterUserId,
                raid.FromBroadcasterUserLogin,
                raid.FromBroadcasterUserName,
                raid.Viewers,
                notification.ObservedAtUtc
            ),
            TwitchStreamOnlineEvent online => PublishAsync(
                notification,
                "СТРИМ НАЧАЛСЯ",
                string.IsNullOrWhiteSpace(online.Type)
                    ? $"{DisplayName(online.BroadcasterUserName, online.BroadcasterUserLogin)} начал трансляцию"
                    : $"{DisplayName(online.BroadcasterUserName, online.BroadcasterUserLogin)} начал трансляцию ({online.Type})",
                online.BroadcasterUserId,
                online.BroadcasterUserLogin,
                online.BroadcasterUserName,
                0,
                online.StartedAt
            ),
            TwitchStreamOfflineEvent offline => PublishAsync(
                notification,
                "СТРИМ ЗАВЕРШЁН",
                $"{DisplayName(offline.BroadcasterUserName, offline.BroadcasterUserLogin)} завершил трансляцию",
                offline.BroadcasterUserId,
                offline.BroadcasterUserLogin,
                offline.BroadcasterUserName,
                0,
                notification.ObservedAtUtc
            ),
            TwitchChannelModeratorAddedEvent moderator => PublishAsync(
                notification,
                "НОВЫЙ МОДЕРАТОР",
                $"{DisplayName(moderator.UserName, moderator.UserLogin)} назначен модератором",
                moderator.UserId,
                moderator.UserLogin,
                moderator.UserName,
                0,
                notification.ObservedAtUtc
            ),
            TwitchChannelVipAddedEvent vip => PublishAsync(
                notification,
                "НОВЫЙ VIP",
                $"{DisplayName(vip.UserName, vip.UserLogin)} получил VIP",
                vip.UserId,
                vip.UserLogin,
                vip.UserName,
                0,
                notification.ObservedAtUtc
            ),
            // RewardAlertConsumer already dispatches redemption alerts by the
            // reward-specific routing key. Do not emit a second generic overlay.
            TwitchChannelPointsCustomRewardRedemptionAddedEvent => Task.CompletedTask,
            _ => LogUnsupportedAsync(notification),
        };
    }

    private Task OnEventReceivedAsync(object? sender, TwitchEventSubNotification notification) =>
        DispatchAsync(notification);

    private Task PublishAsync(
        TwitchEventSubNotification notification,
        string title,
        string message,
        string? userId,
        string? userLogin,
        string? userName,
        int viewers,
        DateTimeOffset? occurredAtUtc
    )
    {
        var eventId = string.IsNullOrWhiteSpace(notification.MessageId)
            ? Guid.NewGuid().ToString("N")
            : notification.MessageId;
        var timestamp =
            occurredAtUtc is { } occurred && occurred != DateTimeOffset.MinValue
                ? occurred
                : notification.ObservedAtUtc ?? DateTimeOffset.UtcNow;

        return notifier.TwitchEventAlert(
            eventId,
            notification.SubscriptionType,
            title,
            message,
            userId ?? string.Empty,
            userLogin ?? string.Empty,
            userName ?? string.Empty,
            viewers,
            timestamp.ToString("O")
        );
    }

    private Task LogUnsupportedAsync(TwitchEventSubNotification notification)
    {
        logger.LogDebug(
            "Twitch EventSub notification {SubscriptionType} has no overlay alert mapping",
            notification.SubscriptionType
        );
        return Task.CompletedTask;
    }

    private static string DisplayName(string? userName, string? userLogin) =>
        !string.IsNullOrWhiteSpace(userName) ? userName
        : !string.IsNullOrWhiteSpace(userLogin) ? userLogin
        : "Twitch user";
}
