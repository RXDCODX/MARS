using System.Threading.Channels;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Telegramus;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Доступ подписчика к собственной очереди нужен не только gRPC-стриму.
/// </summary>
/// <remarks>
/// Очередь и обратное чтение из неё были <c>internal</c>: единственным
/// потребителем считался <c>PumpAsync</c>, пишущий в <c>IAsyncStreamWriter</c>.
/// Реле оверлейного хаба тоже читает из подписки, но лежит в другом
/// сервисе, и без публичного <c>Reader</c> оно не запустилось бы вовсе.
/// </remarks>
public class GrpcSubscriptionReaderTests
{
    private static async Task<List<TelegramusEvent>> ReadAsync(
        ChannelReader<TelegramusEvent> reader,
        int count,
        CancellationToken cancellationToken
    )
    {
        var received = new List<TelegramusEvent>(count);

        for (var i = 0; i < count; i++)
        {
            received.Add(await reader.ReadAsync(cancellationToken));
        }

        return received;
    }

    private static TelegramusEvent Credit(int index) =>
        new() { Adhd = new AdhdEvent { Seconds = index } };

    [Fact]
    public async Task Reader_yields_broadcast_messages_in_order()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        using var subscription = broadcaster.Subscribe("overlay");
        var reader = subscription.Reader;

        await broadcaster.BroadcastAsync(Credit(1));
        await broadcaster.BroadcastAsync(Credit(2));
        await broadcaster.BroadcastAsync(Credit(3));

        var received = await ReadAsync(reader, 3, TestContext.Current.CancellationToken);

        Assert.Equal([1, 2, 3], received.Select(e => e.Adhd.Seconds));
    }

    /// <summary>
    /// Ёмкость очереди подписки — это размер допустимого отставания подписчика,
    /// а не размер сообщения. Реле оверлея отстаёт на время сетевой отправки,
    /// поэтому его очередь должна быть больше стандартной.
    /// </summary>
    [Fact]
    public async Task Subscribe_with_larger_capacity_absorbs_burst()
    {
        const int burst = 200;

        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        using var subscription = broadcaster.Subscribe("overlay", 512);
        var reader = subscription.Reader;

        for (var i = 0; i < burst; i++)
        {
            await broadcaster.BroadcastAsync(Credit(i));
        }

        var received = await ReadAsync(reader, burst, TestContext.Current.CancellationToken);

        Assert.Equal(burst, received.Count);
    }

    /// <summary>
    /// Фиксирует границу: при стандартной ёмкости и более длинной серии
    /// сообщения теряются молча. Пока это так, реле обязано подписываться с
    /// увеличенной ёмкостью — иначе один медленный OBS-клиент уронил бы события
    /// всем остальным, а не только себе.
    /// </summary>
    [Fact]
    public async Task Default_capacity_drops_messages_beyond_limit()
    {
        const int burst = 200;

        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        using var subscription = broadcaster.Subscribe("overlay");
        var reader = subscription.Reader;

        for (var i = 0; i < burst; i++)
        {
            await broadcaster.BroadcastAsync(Credit(i));
        }

        var received = await ReadAsync(reader, 64, TestContext.Current.CancellationToken);

        Assert.Equal(64, received.Count);
        Assert.Equal(63, received[^1].Adhd.Seconds);
    }
}
