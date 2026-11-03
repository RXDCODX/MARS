using System.Text.Json;

namespace MARS.Shared.Messaging;

/// <summary>
/// Async event raised by <see cref="TwitchEventSubConsumer"/> for each notification.
/// </summary>
public delegate Task TwitchEventSubNotificationHandler(
    object? sender,
    TwitchEventSubNotification notification
);

/// <summary>
/// A Twitch EventSub notification whose EventData has already been deserialized
/// to the model selected by SubscriptionType.
/// </summary>
public sealed record TwitchEventSubNotification(
    int SchemaVersion,
    string? MessageId,
    string? MessageType,
    string SubscriptionType,
    string? SubscriptionVersion,
    string? SubscriptionId,
    string? SessionId,
    string? EventName,
    DateTimeOffset? ObservedAtUtc,
    string? SourceService,
    string? TraceParent,
    object EventData,
    bool IsKnownEventType,
    JsonElement RawMessage,
    JsonElement RawEvent
)
{
    /// <summary>The concrete CLR type selected by the consumer's event type registry.</summary>
    public Type EventClrType => EventData.GetType();

    /// <summary>Returns the deserialized event when it matches the requested DTO type.</summary>
    public TEvent? GetEvent<TEvent>()
        where TEvent : class => EventData as TEvent;
}

/// <summary>channel.raid notification payload.</summary>
public sealed record TwitchChannelRaidEvent
{
    public string? FromBroadcasterUserId { get; init; }
    public string? FromBroadcasterUserLogin { get; init; }
    public string? FromBroadcasterUserName { get; init; }
    public string? ToBroadcasterUserId { get; init; }
    public string? ToBroadcasterUserLogin { get; init; }
    public string? ToBroadcasterUserName { get; init; }
    public int Viewers { get; init; }
}

/// <summary>stream.online notification payload.</summary>
public sealed record TwitchStreamOnlineEvent
{
    public string? Id { get; init; }
    public string? BroadcasterUserId { get; init; }
    public string? BroadcasterUserLogin { get; init; }
    public string? BroadcasterUserName { get; init; }
    public string? Type { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public bool IsMature { get; init; }
}

/// <summary>stream.offline notification payload.</summary>
public sealed record TwitchStreamOfflineEvent
{
    public string? BroadcasterUserId { get; init; }
    public string? BroadcasterUserLogin { get; init; }
    public string? BroadcasterUserName { get; init; }
}

/// <summary>channel.channel_points_custom_reward_redemption.add notification payload.</summary>
public sealed record TwitchChannelPointsCustomRewardRedemptionAddedEvent
{
    public string? Id { get; init; }
    public string? BroadcasterUserId { get; init; }
    public string? BroadcasterUserLogin { get; init; }
    public string? BroadcasterUserName { get; init; }
    public string? UserId { get; init; }
    public string? UserLogin { get; init; }
    public string? UserName { get; init; }
    public string? UserInput { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset RedeemedAt { get; init; }
    public TwitchChannelPointsReward? Reward { get; init; }
}

/// <summary>Reward details embedded in a redemption notification.</summary>
public sealed record TwitchChannelPointsReward
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Prompt { get; init; }
    public int Cost { get; init; }
    public bool IsUserInputRequired { get; init; }
}

/// <summary>channel.moderator.add notification payload.</summary>
public sealed record TwitchChannelModeratorAddedEvent
{
    public string? BroadcasterUserId { get; init; }
    public string? BroadcasterUserLogin { get; init; }
    public string? BroadcasterUserName { get; init; }
    public string? UserId { get; init; }
    public string? UserLogin { get; init; }
    public string? UserName { get; init; }
}

/// <summary>channel.vip.add notification payload.</summary>
public sealed record TwitchChannelVipAddedEvent
{
    public string? BroadcasterUserId { get; init; }
    public string? BroadcasterUserLogin { get; init; }
    public string? BroadcasterUserName { get; init; }
    public string? UserId { get; init; }
    public string? UserLogin { get; init; }
    public string? UserName { get; init; }
}

/// <summary>channel.follow v2 notification payload.</summary>
public sealed record TwitchChannelFollowEvent
{
    public string? BroadcasterUserId { get; init; }
    public string? BroadcasterUserLogin { get; init; }
    public string? BroadcasterUserName { get; init; }
    public string? UserId { get; init; }
    public string? UserLogin { get; init; }
    public string? UserName { get; init; }
    public DateTimeOffset FollowedAt { get; init; }
}
