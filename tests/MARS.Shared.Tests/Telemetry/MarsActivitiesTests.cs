using System.Diagnostics;
using MARS.Shared.Telemetry;

namespace MARS.Shared.Tests.Telemetry;

/// <summary>
/// Трассировки MARS: имена спанов и теги, по которым в Tempo ищут причину
/// сбоя. Проверяется именно то, что попадёт в трек — с читаемым именем и
/// заполненными тегами, иначе поиск по трейсу ничего не находит.
/// </summary>
public class MarsActivitiesTests
{
    private static readonly List<ActivityListener> Listeners = [];

    public MarsActivitiesTests() => Subscribe();

    [Fact]
    public void InterServiceCallIsTracedAsClientSpan()
    {
        using var activity = Start(activity =>
            MarsActivities.StartInterServiceCall("media-storage", "GET", "api/media/info")
        );

        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Client, activity!.Kind);
        Assert.Equal("HTTP GET media-storage/api/media/info", activity.DisplayName);
        Assert.Equal("media-storage", activity.GetTagItem("peer.service"));
        Assert.Equal("GET", activity.GetTagItem("http.method"));
        Assert.Equal("api/media/info", activity.GetTagItem("http.url"));
    }

    [Fact]
    public void RabbitMqPublishIsTracedAsProducerSpan()
    {
        using var activity = Start(activity =>
            MarsActivities.StartRabbitMqPublish("mars.events", "twitch.reward.redeemed")
        );

        Assert.Equal(ActivityKind.Producer, activity!.Kind);
        Assert.Equal("rabbitmq.publish mars.events/twitch.reward.redeemed", activity.DisplayName);
        Assert.Equal("rabbitmq", activity.GetTagItem("messaging.system"));
        Assert.Equal("mars.events", activity.GetTagItem("messaging.destination"));
        Assert.Equal(
            "twitch.reward.redeemed",
            activity.GetTagItem("messaging.rabbitmq.routing_key")
        );
    }

    [Fact]
    public void RabbitMqConsumeIsTracedAsConsumerSpan()
    {
        using var activity = Start(activity => MarsActivities.StartRabbitMqConsume("alerts-queue"));

        Assert.Equal(ActivityKind.Consumer, activity!.Kind);
        Assert.Equal("rabbitmq.consume alerts-queue", activity.DisplayName);
        Assert.Equal("alerts-queue", activity.GetTagItem("messaging.source"));
    }

    [Fact]
    public void EventSubEventIsTracedByType()
    {
        using var activity = Start(activity =>
            MarsActivities.StartEventSubReceived("channel.channel_points_custom_reward_redemption")
        );

        Assert.Equal(ActivityKind.Consumer, activity!.Kind);
        Assert.Equal(
            "twitch.eventsub.channel.channel_points_custom_reward_redemption",
            activity.DisplayName
        );
        Assert.Equal(
            "channel.channel_points_custom_reward_redemption",
            activity.GetTagItem("twitch.event_type")
        );
    }

    [Fact]
    public void RewardProcessingCarriesRewardAndUser()
    {
        using var activity = Start(activity =>
            MarsActivities.StartRewardProcessing("reward-1", "Аяка", "123456789")
        );

        Assert.Equal("twitch.reward.processing", activity!.DisplayName);
        Assert.Equal("reward-1", activity.GetTagItem("twitch.reward.id"));
        Assert.Equal("Аяка", activity.GetTagItem("twitch.reward.title"));
        Assert.Equal("123456789", activity.GetTagItem("twitch.user.id"));
    }

    [Fact]
    public void ReceivedCommandCarriesPlatformAndText()
    {
        using var activity = Start(activity =>
            MarsActivities.StartCommandReceived("twitch", "pyro", "!sr трек")
        );

        Assert.Equal("command.received", activity!.DisplayName);
        Assert.Equal("twitch", activity.GetTagItem("command.platform"));
        Assert.Equal("pyro", activity.GetTagItem("command.user"));
        Assert.Equal("!sr трек", activity.GetTagItem("command.raw_message"));
    }

    [Fact]
    public void ParsedCommandJoinsArguments()
    {
        using var activity = Start(activity =>
            MarsActivities.StartCommandParsed("sr", ["трек", "second"])
        );

        Assert.Equal("command.parsed", activity!.DisplayName);
        Assert.Equal("sr", activity.GetTagItem("command.name"));
        Assert.Equal("трек second", activity.GetTagItem("command.args"));
        Assert.Equal(2, activity.GetTagItem("command.args_count"));
    }

    [Fact]
    public void RoutedCommandKeepsTargetService()
    {
        using var activity = Start(activity =>
            MarsActivities.StartCommandRouted("sr", "sound-request")
        );

        Assert.Equal("command.routed", activity!.DisplayName);
        Assert.Equal("sound-request", activity.GetTagItem("command.target_service"));
    }

    [Fact]
    public void AlertProcessingIsTracedByType()
    {
        using var activity = Start(activity =>
            MarsActivities.StartAlertProcessing("meme", "random-meme")
        );

        Assert.Equal("alert.meme", activity!.DisplayName);
        Assert.Equal("meme", activity.GetTagItem("alert.type"));
        Assert.Equal("random-meme", activity.GetTagItem("alert.source"));
    }

    [Fact]
    public void GrpcBroadcastIsTracedAsProducerSpan()
    {
        using var activity = Start(activity => MarsActivities.StartGrpcBroadcast("PlayMedia"));

        Assert.Equal(ActivityKind.Producer, activity!.Kind);
        Assert.Equal("grpc.broadcast.PlayMedia", activity.DisplayName);
        Assert.Equal("grpc", activity.GetTagItem("messaging.system"));
        Assert.Equal("PlayMedia", activity.GetTagItem("rpc.method"));
    }

    /// <summary>
    /// Без слушателя ActivitySource не создаёт спанов и возвращает null — так
    /// и должно быть при выключенной трассировке, без исключений и затрат.
    /// </summary>
    [Fact]
    public void WithoutListenerNothingIsCreated()
    {
        foreach (var listener in Listeners)
        {
            listener.Dispose();
        }

        Listeners.Clear();

        Assert.Null(MarsActivities.StartInterServiceCall("media-storage", "GET", "api/media/info"));
        Assert.Null(MarsActivities.StartCommandReceived("twitch", "pyro", "!sr"));

        Subscribe();
    }

    private void Subscribe()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("MARS.", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        Listeners.Add(listener);
    }

    private static Activity? Start(Func<Activity?, Activity?> factory)
    {
        var current = Activity.Current;
        Activity.Current = null;

        return factory(null);
    }
}
