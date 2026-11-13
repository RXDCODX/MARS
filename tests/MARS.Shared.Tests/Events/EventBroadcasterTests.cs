using System.Threading.Channels;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Shared.Tests.Grpc;

namespace MARS.Shared.Tests.Events;

public class EventBroadcasterTests
{
    [Fact]
    public async Task BroadcastAsync_DeliversEventToEverySubscriber()
    {
        var broadcaster = new EventBroadcaster<StringValue>();
        using var first = broadcaster.Subscribe();
        using var second = broadcaster.Subscribe();

        await broadcaster.BroadcastAsync(new StringValue { Value = "alert" });

        Assert.Equal("alert", (await ReadQueuedAsync(first)).Value);
        Assert.Equal("alert", (await ReadQueuedAsync(second)).Value);
    }

    [Fact]
    public async Task BroadcastExceptAsync_KeepsEventFromExcludedSubscriberOnly()
    {
        var broadcaster = new EventBroadcaster<StringValue>();
        using var caller = broadcaster.Subscribe();
        using var other = broadcaster.Subscribe();

        await broadcaster.BroadcastExceptAsync(caller.Id, new StringValue { Value = "progress" });

        Assert.True(other.Queue.Reader.TryRead(out var delivered));
        Assert.Equal("progress", delivered.Value);
        Assert.False(caller.Queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Dispose_StopsDeliveryToSubscriber()
    {
        var broadcaster = new EventBroadcaster<StringValue>();
        var subscription = broadcaster.Subscribe();
        subscription.Dispose();

        await broadcaster.BroadcastAsync(new StringValue { Value = "after-dispose" });

        Assert.Equal(0, broadcaster.SubscriberCount);
        Assert.False(subscription.Queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task PumpAsync_WritesQueuedEventsToStream()
    {
        var broadcaster = new EventBroadcaster<StringValue>();
        using var subscription = broadcaster.Subscribe();
        using var cancellation = new CancellationTokenSource();
        var channel = Channel.CreateUnbounded<StringValue>();
        var writer = new RecordingAsyncStreamWriter<StringValue>(channel);

        Assert.True(broadcaster.TryEnqueue(subscription, new StringValue { Value = "first" }));
        var pump = subscription.PumpAsync(writer, cancellation.Token);

        var written = await channel.Reader.ReadAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await pump;

        Assert.Equal("first", written.Value);
    }

    [Fact]
    public async Task PumpAsync_StopsWhenSubscriptionIsDisposed()
    {
        var broadcaster = new EventBroadcaster<StringValue>();
        var subscription = broadcaster.Subscribe();
        var channel = Channel.CreateUnbounded<StringValue>();
        var writer = new RecordingAsyncStreamWriter<StringValue>(channel);

        var pump = subscription.PumpAsync(writer, TestContext.Current.CancellationToken);
        subscription.Dispose();

        await pump;

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    [Fact]
    public async Task BroadcastAsync_DropsEventsWhenSubscriberQueueIsFull()
    {
        var broadcaster = new EventBroadcaster<StringValue>(queueCapacity: 1);
        using var subscription = broadcaster.Subscribe();

        await broadcaster.BroadcastAsync(new StringValue { Value = "kept" });
        await broadcaster.BroadcastAsync(new StringValue { Value = "dropped" });

        Assert.True(subscription.Queue.Reader.TryRead(out var delivered));
        Assert.Equal("kept", delivered.Value);
        Assert.False(subscription.Queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task BroadcastAsync_WithoutSubscribers_DoesNothing()
    {
        var broadcaster = new EventBroadcaster<StringValue>();

        await broadcaster.BroadcastAsync(new StringValue { Value = "nobody-listening" });

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    [Fact]
    public void TryEnqueue_ReturnsFalseForForeignSubscription()
    {
        var broadcaster = new EventBroadcaster<StringValue>();
        var otherBroadcaster = new EventBroadcaster<StringValue>();
        using var foreign = otherBroadcaster.Subscribe();

        var enqueued = broadcaster.TryEnqueue(foreign, new StringValue { Value = "x" });

        Assert.False(enqueued);
    }

    private static async Task<TMessage> ReadQueuedAsync<TMessage>(
        EventSubscription<TMessage> subscription
    )
        where TMessage : class, IMessage<TMessage>, new()
    {
        return await subscription.Queue.Reader.ReadAsync(TestContext.Current.CancellationToken);
    }
}
