using System.Threading.Channels;
using Grpc.Core;

namespace MARS.Shared.Tests.Grpc;

public sealed class RecordingAsyncStreamWriter<TMessage>(Channel<TMessage> channel)
    : IServerStreamWriter<TMessage>
{
    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(TMessage message)
    {
        return channel.Writer.WriteAsync(message).AsTask();
    }

    public Task WriteAsync(TMessage message, CancellationToken cancellationToken)
    {
        return channel.Writer.WriteAsync(message, cancellationToken).AsTask();
    }
}
