using System.Diagnostics.Metrics;

namespace MARS.Shared.Telemetry;

public static class MarsMetrics
{
    private static readonly Meter Core = new("MARS.Core", "1.0.0");
    private static readonly Meter Twitch = new("MARS.Twitch", "1.0.0");
    private static readonly Meter Media = new("MARS.Media", "1.0.0");
    private static readonly Meter Alerts = new("MARS.Alerts", "1.0.0");
    private static readonly Meter RabbitMq = new("MARS.RabbitMQ", "1.0.0");
    private static readonly Meter Commands = new("MARS.Commands", "1.0.0");

    // Core
    public static readonly Counter<long> InterServiceCalls = Core.CreateCounter<long>(
        "mars.interservice.calls",
        description: "Inter-service HTTP calls"
    );
    public static readonly Histogram<double> InterServiceLatency = Core.CreateHistogram<double>(
        "mars.interservice.latency_ms",
        unit: "ms",
        description: "Inter-service call latency"
    );

    // Twitch
    public static readonly Counter<long> TwitchEventsReceived = Twitch.CreateCounter<long>(
        "mars.twitch.events.received",
        description: "Twitch events received"
    );
    public static readonly Counter<long> TwitchRewardsRedeemed = Twitch.CreateCounter<long>(
        "mars.twitch.rewards.redeemed",
        description: "Twitch rewards redeemed"
    );
    public static readonly Counter<long> TwitchRewardsProcessed = Twitch.CreateCounter<long>(
        "mars.twitch.rewards.processed",
        description: "Rewards successfully processed"
    );
    public static readonly Counter<long> TwitchRewardsFailed = Twitch.CreateCounter<long>(
        "mars.twitch.rewards.failed",
        description: "Rewards failed processing"
    );
    public static readonly Histogram<double> TwitchEventProcessingTime =
        Twitch.CreateHistogram<double>(
            "mars.twitch.event.processing_ms",
            unit: "ms",
            description: "Twitch event processing time"
        );

    // Media
    public static readonly Counter<long> SoundRequestTracksPlayed = Media.CreateCounter<long>(
        "mars.media.tracks.played",
        description: "Tracks played via SoundRequest"
    );
    public static readonly Counter<long> TtsMessagesProcessed = Media.CreateCounter<long>(
        "mars.media.tts.processed",
        description: "TTS messages processed"
    );

    // Alerts
    public static readonly Counter<long> AlertsSent = Alerts.CreateCounter<long>(
        "mars.alerts.sent",
        description: "Alerts sent via TelegramusHub"
    );
    public static readonly Counter<long> AlertsByType = Alerts.CreateCounter<long>(
        "mars.alerts.by_type",
        description: "Alerts by type"
    );

    // RabbitMQ
    public static readonly Counter<long> RabbitMqPublished = RabbitMq.CreateCounter<long>(
        "mars.rabbitmq.published",
        description: "Messages published"
    );
    public static readonly Counter<long> RabbitMqConsumed = RabbitMq.CreateCounter<long>(
        "mars.rabbitmq.consumed",
        description: "Messages consumed"
    );
    public static readonly Counter<long> RabbitMqConsumerErrors = RabbitMq.CreateCounter<long>(
        "mars.rabbitmq.consumer_errors",
        description: "Consumer errors"
    );
    public static readonly Counter<long> RabbitMqUnhandledMessages = RabbitMq.CreateCounter<long>(
        "mars.rabbitmq.unhandled_messages",
        description: "Messages with no registered handler"
    );

    // Commands
    public static readonly Counter<long> CommandsExecuted = Commands.CreateCounter<long>(
        "mars.commands.executed",
        description: "Commands executed"
    );
    public static readonly Counter<long> CommandsByPlatform = Commands.CreateCounter<long>(
        "mars.commands.by_platform",
        description: "Commands by platform"
    );
    public static readonly Counter<long> CommandsByName = Commands.CreateCounter<long>(
        "mars.commands.by_name",
        description: "Commands by name"
    );
    public static readonly Counter<long> CommandsByTarget = Commands.CreateCounter<long>(
        "mars.commands.by_target",
        description: "Commands by target service"
    );
    public static readonly Counter<long> CommandsSucceeded = Commands.CreateCounter<long>(
        "mars.commands.succeeded",
        description: "Commands succeeded"
    );
    public static readonly Counter<long> CommandsFailed = Commands.CreateCounter<long>(
        "mars.commands.failed",
        description: "Commands failed"
    );
    public static readonly Histogram<double> CommandLatency = Commands.CreateHistogram<double>(
        "mars.commands.latency_ms",
        unit: "ms",
        description: "Command latency"
    );
    public static readonly Counter<long> CommandsUnknown = Commands.CreateCounter<long>(
        "mars.commands.unknown",
        description: "Unknown commands"
    );
}
