using MARS.Shared.Concurrency;

namespace MARS.Shared.Tests.Concurrency;

/// <summary>
/// Блокер №16: прежняя обёртка <c>SemaphoreSlim</c> со счётчиком использований
/// отдавала из <c>GetOrAdd</c> уже задиспозитленный семафор (счётчик уходил в -1).
/// <see cref="KeyedAsyncLock"/> обязан сериализовать по ключу и не диспозить
/// семафор, пока на запись ссылается хоть один поток.
/// </summary>
public class KeyedAsyncLockTests
{
    [Fact]
    public async Task AcquireAsync_WhenSameKey_ThenCriticalSectionsDoNotOverlap()
    {
        var keyedLock = new KeyedAsyncLock();
        var ct = TestContext.Current.CancellationToken;
        var concurrent = 0;
        var maxConcurrent = 0;
        var iterations = 200;

        var workers = Enumerable
            .Range(0, 16)
            .Select(_ =>
                Task.Run(
                    async () =>
                    {
                        for (var i = 0; i < iterations; i++)
                        {
                            using (await keyedLock.AcquireAsync("roll", ct))
                            {
                                var current = Interlocked.Increment(ref concurrent);
                                InterlockedMax(ref maxConcurrent, current);
                                await Task.Yield();
                                Interlocked.Decrement(ref concurrent);
                            }
                        }
                    },
                    ct
                )
            )
            .ToArray();

        await Task.WhenAll(workers);

        Assert.Equal(1, maxConcurrent);
    }

    [Fact]
    public async Task AcquireAsync_WhenDifferentKeys_ThenRunConcurrently()
    {
        var keyedLock = new KeyedAsyncLock();
        var ct = TestContext.Current.CancellationToken;
        using var first = await keyedLock.AcquireAsync("a", ct);
        using var second = await keyedLock.AcquireAsync("b", ct);

        // Второй ключ не должен ждать первый: если бы лок был глобальным,
        // AcquireAsync("b") вернулся бы только после освобождения "a".
        var isNotBlocked = second is not null;

        Assert.True(isNotBlocked);
    }

    [Fact]
    public async Task AcquireAsync_WhenKeyIsReleased_ThenEntriesAreRemoved()
    {
        var keyedLock = new KeyedAsyncLock();
        var ct = TestContext.Current.CancellationToken;
        var handle = await keyedLock.AcquireAsync("temp", ct);

        Assert.Equal(1, keyedLock.TrackedKeyCount);

        handle.Dispose();

        Assert.Equal(0, keyedLock.TrackedKeyCount);
    }

    [Fact]
    public async Task AcquireAsync_WhenBodyThrows_ThenKeyIsStillReleased()
    {
        var keyedLock = new KeyedAsyncLock();
        var ct = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using (await keyedLock.AcquireAsync("boom", ct))
            {
                throw new InvalidOperationException("body");
            }
        });

        Assert.Equal(0, keyedLock.TrackedKeyCount);
    }

    [Fact]
    public async Task AcquireAsync_WhenHandleDisposedTwice_ThenRefCountIsNotBroken()
    {
        var keyedLock = new KeyedAsyncLock();
        var ct = TestContext.Current.CancellationToken;
        var handle = await keyedLock.AcquireAsync("double", ct);

        handle.Dispose();
        handle.Dispose();

        Assert.Equal(0, keyedLock.TrackedKeyCount);

        // Следующий захват обязан получить живой семафор (а не задиспозитенный).
        using var next = await keyedLock.AcquireAsync("double", ct);
        Assert.Equal(1, keyedLock.TrackedKeyCount);
    }

    [Fact]
    public async Task AcquireAsync_WhenCancelledWhileWaiting_ThenKeyIsNotLeaked()
    {
        var keyedLock = new KeyedAsyncLock();
        var ct = TestContext.Current.CancellationToken;
        var hold = await keyedLock.AcquireAsync("cancel", ct);
        using var cts = new CancellationTokenSource();

        var waiting = keyedLock.AcquireAsync("cancel", cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await waiting);
        hold.Dispose();

        Assert.Equal(0, keyedLock.TrackedKeyCount);
    }

    [Fact]
    public async Task AcquireAsync_WhenKeyIsNullOrWhitespace_ThenThrows()
    {
        var keyedLock = new KeyedAsyncLock();
        var ct = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await keyedLock.AcquireAsync("   ", ct)
        );
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        do
        {
            current = Volatile.Read(ref target);
            if (value <= current)
            {
                return;
            }
        } while (Interlocked.CompareExchange(ref target, value, current) != current);
    }
}
