using MARS.Platform.Infrastructure.Events;

namespace MARS.Shared.Tests.Events;

public class EventBroadcasterTests
{
    private sealed record TestEvent(string Value);

    [Fact]
    public async Task BroadcastAsync_DeliversEventToEverySubscriber()
    {
        var broadcaster = new EventBroadcaster<TestEvent>();
        using var first = broadcaster.Subscribe();
        using var second = broadcaster.Subscribe();

        await broadcaster.BroadcastAsync(new TestEvent("alert"));

        Assert.Equal("alert", (await ReadQueuedAsync(first)).Value);
        Assert.Equal("alert", (await ReadQueuedAsync(second)).Value);
    }

    [Fact]
    public async Task BroadcastExceptAsync_KeepsEventFromExcludedSubscriberOnly()
    {
        var broadcaster = new EventBroadcaster<TestEvent>();
        using var caller = broadcaster.Subscribe();
        using var other = broadcaster.Subscribe();

        await broadcaster.BroadcastExceptAsync(caller.Id, new TestEvent("progress"));

        Assert.True(other.Reader.TryRead(out var delivered));
        Assert.Equal("progress", delivered.Value);
        Assert.False(caller.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Dispose_StopsDeliveryToSubscriber()
    {
        var broadcaster = new EventBroadcaster<TestEvent>();
        var subscription = broadcaster.Subscribe();
        subscription.Dispose();

        await broadcaster.BroadcastAsync(new TestEvent("after-dispose"));

        Assert.Equal(0, broadcaster.SubscriberCount);
        Assert.False(subscription.Reader.TryRead(out _));
    }

    [Fact]
    public async Task BroadcastAsync_DropsEventsWhenSubscriberQueueIsFull()
    {
        var broadcaster = new EventBroadcaster<TestEvent>(queueCapacity: 1);
        using var subscription = broadcaster.Subscribe();

        await broadcaster.BroadcastAsync(new TestEvent("kept"));
        await broadcaster.BroadcastAsync(new TestEvent("dropped"));

        Assert.True(subscription.Reader.TryRead(out var delivered));
        Assert.Equal("kept", delivered.Value);
        Assert.False(subscription.Reader.TryRead(out _));
    }

    [Fact]
    public async Task BroadcastAsync_WithoutSubscribers_DoesNothing()
    {
        var broadcaster = new EventBroadcaster<TestEvent>();

        await broadcaster.BroadcastAsync(new TestEvent("nobody-listening"));

        Assert.Equal(0, broadcaster.SubscriberCount);
    }

    [Fact]
    public void TryEnqueue_ReturnsFalseForForeignSubscription()
    {
        var broadcaster = new EventBroadcaster<TestEvent>();
        var otherBroadcaster = new EventBroadcaster<TestEvent>();
        using var foreign = otherBroadcaster.Subscribe();

        var enqueued = broadcaster.TryEnqueue(foreign, new TestEvent("x"));

        Assert.False(enqueued);
    }

    private static async Task<TestEvent> ReadQueuedAsync(EventSubscription<TestEvent> subscription)
    {
        return await subscription.Reader.ReadAsync(TestContext.Current.CancellationToken);
    }
}
