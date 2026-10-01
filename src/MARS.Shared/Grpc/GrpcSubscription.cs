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
