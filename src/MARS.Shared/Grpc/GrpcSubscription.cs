using System.Threading.Channels;
using Google.Protobuf;

namespace MARS.Shared.Grpc;

public sealed class GrpcSubscription<TMessage> : IDisposable
    where TMessage : class, IMessage<TMessage>, new()
{
    private readonly GrpcEventBroadcaster<TMessage> _broadcaster;
    private bool _disposed;

    internal GrpcSubscription(
        GrpcEventBroadcaster<TMessage> broadcaster,
        string id,
        Channel<TMessage> queue
    )
    {
        _broadcaster = broadcaster;
        Id = id;
        Queue = queue;
    }

    public string Id { get; }

    internal Channel<TMessage> Queue { get; }

    /// <summary>
    /// Чтение собственной очереди подписки.
    /// </summary>
    /// <remarks>
    /// Открыто наружу, а не только внутрь сборки: очередь читает не только
    /// gRPC-стрим через <c>PumpAsync</c>, но и реле оверлейного хаба, которое
    /// лежит в сервисе <c>MARS.Alerts</c> и потому не видит <c>internal</c>.
    /// Отдаётся <c>ChannelReader</c>, а не сама <c>Channel</c>: запись в очередь
    /// подписчику не нужна, а лишняя половина API была бы приглашением ею
    /// воспользоваться.
    /// </remarks>
    public ChannelReader<TMessage> Reader => Queue.Reader;

    public void Dispose()
    {
        if (!_disposed)
        {
            _broadcaster.Unsubscribe(Id);
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
