using System.Text.Json;

namespace MARS.Shared.Messaging;

/// <summary>
/// Envelope published by MARS.TwitchCore for every raw Twitch EventSub WebSocket frame.
/// The Raw property contains the original Twitch JSON frame, including fields unknown
/// to the current application or TwitchLib version.
/// </summary>
public sealed record TwitchEventSubEnvelope
{
    public int SchemaVersion { get; init; }
    public string? MessageId { get; init; }
    public string? MessageType { get; init; }
    public string? SubscriptionType { get; init; }
    public string? SubscriptionVersion { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SessionId { get; init; }
    public string? EventName { get; init; }
    public DateTimeOffset? ObservedAtUtc { get; init; }
    public string? SourceService { get; init; }
    public string? TraceParent { get; init; }
    public JsonElement Raw { get; init; }
}

/// <summary>
/// Metadata accompanying a deserialized Twitch EventSub notification.
/// RawMessage and RawEvent let consumers access fields not represented by TEvent.
/// </summary>
public sealed record TwitchEventSubContext(
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
    JsonElement RawMessage,
    JsonElement RawEvent
);

/// <summary>
/// Twitch EventSub subscription type identifiers currently used by MARS.TwitchCore.
/// Strings are kept here to avoid coupling MARS.Shared consumers to TwitchLib.
/// </summary>
public static class TwitchEventSubTypes
{
    public const string ChannelRaid = "channel.raid";
    public const string StreamOnline = "stream.online";
    public const string StreamOffline = "stream.offline";
    public const string ChannelPointsCustomRewardRedemptionAdd =
        "channel.channel_points_custom_reward_redemption.add";
    public const string ChannelModeratorAdd = "channel.moderator.add";
    public const string ChannelVipAdd = "channel.vip.add";
    public const string ChannelFollow = "channel.follow";
}
