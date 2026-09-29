using MARS.TwitchCore.Services.HelloVideos;

namespace MARS.Microservices.Tests.HelloVideos;

public class RecentMessageTrackerTests
{
    [Fact]
    public void TryMarkSeen_WhenFirstTime_ReturnsFalse()
    {
        var tracker = new RecentMessageTracker(capacity: 4);

        var result = tracker.TryMarkSeen("msg-1");

        Assert.False(result);
        Assert.Equal(1, tracker.Count);
    }

    [Fact]
    public void TryMarkSeen_WhenAlreadySeen_ReturnsTrue()
    {
        var tracker = new RecentMessageTracker(capacity: 4);
        tracker.TryMarkSeen("msg-1");

        var result = tracker.TryMarkSeen("msg-1");

        Assert.True(result);
        Assert.Equal(1, tracker.Count);
    }

    [Fact]
    public void TryMarkSeen_WhenOverCapacity_EvictsOldest()
    {
        // Регрессия аудита №17: вытесняться должен самый старый элемент.
        // При неверной реализации вытеснялся бы только что добавленный,
        // и oldest оставался бы в кэше навсегда.
        var tracker = new RecentMessageTracker(capacity: 2);
        tracker.TryMarkSeen("oldest");
        tracker.TryMarkSeen("middle");
        tracker.TryMarkSeen("newest");

        Assert.Equal(2, tracker.Count);
        // newest и middle остались в кэше.
        Assert.True(tracker.TryMarkSeen("newest"));
        Assert.True(tracker.TryMarkSeen("middle"));
        // oldest вытеснен → вернёт false (не найден). Проверяем последним,
        // т.к. повторный TryMarkSeen("oldest") сам вытеснит следующий по FIFO элемент.
        Assert.False(tracker.TryMarkSeen("oldest"));
    }

    [Fact]
    public void TryMarkSeen_WhenConcurrentSameId_AddsExactlyOnce()
    {
        var tracker = new RecentMessageTracker(capacity: 64);
        var results = new bool[32];

        Parallel.For(
            0,
            results.Length,
            i =>
            {
                results[i] = tracker.TryMarkSeen("shared-id");
            }
        );

        // Метод возвращает true, когда id УЖЕ был виден. Ровно один поток
        // должен был добавить его первым и увидеть false.
        Assert.Single(results.Where(x => !x));
        Assert.Equal(1, tracker.Count);
    }
}
