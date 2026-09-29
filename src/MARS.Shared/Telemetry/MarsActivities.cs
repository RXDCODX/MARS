using System.Diagnostics;

namespace MARS.Shared.Telemetry;

public static class MarsActivities
{
    public static readonly ActivitySource Source = new("MARS.Core");
    public static readonly ActivitySource TwitchSource = new("MARS.Twitch.Events");
    public static readonly ActivitySource CommandSource = new("MARS.Commands");
    public static readonly ActivitySource AlertSource = new("MARS.Alerts");
    public static readonly ActivitySource RabbitMqSource = new("MARS.RabbitMQ");

    public static Activity? StartInterServiceCall(string targetService, string method, string path)
    {
        return Source
            .StartActivity($"HTTP {method} {targetService}/{path}", ActivityKind.Client)
            ?.SetTag("peer.service", targetService)
            .SetTag("http.method", method)
            .SetTag("http.url", path);
    }

    public static Activity? StartRabbitMqPublish(string exchange, string routingKey)
    {
        return RabbitMqSource
            .StartActivity($"rabbitmq.publish {exchange}/{routingKey}", ActivityKind.Producer)
            ?.SetTag("messaging.system", "rabbitmq")
            .SetTag("messaging.destination", exchange)
            .SetTag("messaging.rabbitmq.routing_key", routingKey);
    }

    public static Activity? StartRabbitMqConsume(string queue)
    {
        return RabbitMqSource
            .StartActivity($"rabbitmq.consume {queue}", ActivityKind.Consumer)
            ?.SetTag("messaging.system", "rabbitmq")
            .SetTag("messaging.source", queue);
    }

    // Twitch
    public static Activity? StartEventSubReceived(string eventType)
    {
        return TwitchSource
            .StartActivity($"twitch.eventsub.{eventType}", ActivityKind.Consumer)
            ?.SetTag("twitch.event_type", eventType);
    }

    public static Activity? StartRewardProcessing(
        string rewardId,
        string rewardTitle,
        string userId
    )
    {
        return TwitchSource
            .StartActivity("twitch.reward.processing", ActivityKind.Internal)
            ?.SetTag("twitch.reward.id", rewardId)
            .SetTag("twitch.reward.title", rewardTitle)
            .SetTag("twitch.user.id", userId);
    }

    // Commands
    public static Activity? StartCommandReceived(string platform, string user, string rawMessage)
    {
        return CommandSource
            .StartActivity("command.received", ActivityKind.Consumer)
            ?.SetTag("command.platform", platform)
            .SetTag("command.user", user)
            .SetTag("command.raw_message", rawMessage);
    }

    public static Activity? StartCommandParsed(string commandName, string[] args)
    {
        return CommandSource
            .StartActivity("command.parsed", ActivityKind.Internal)
            ?.SetTag("command.name", commandName)
            .SetTag("command.args", string.Join(" ", args))
            .SetTag("command.args_count", args.Length);
    }

    public static Activity? StartCommandRouted(string commandName, string targetService)
    {
        return CommandSource
            .StartActivity("command.routed", ActivityKind.Internal)
            ?.SetTag("command.name", commandName)
            .SetTag("command.target_service", targetService);
    }

    // Alerts
    public static Activity? StartAlertProcessing(string alertType, string sourceService)
    {
        return AlertSource
            .StartActivity($"alert.{alertType}", ActivityKind.Consumer)
            ?.SetTag("alert.type", alertType)
            .SetTag("alert.source", sourceService);
    }

    public static Activity? StartSignalRBroadcast(string hubMethod)
    {
        return AlertSource
            .StartActivity($"signalr.broadcast.{hubMethod}", ActivityKind.Producer)
            ?.SetTag("signalr.hub", "TelegramusHub")
            .SetTag("signalr.method", hubMethod);
    }
}
