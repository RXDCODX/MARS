using System.Threading.Channels;
using Grpc.Core;
using MARS.Platform.Infrastructure.Events;

namespace MARS.Platform.Infrastructure.Grpc.Extensions;

/// <summary>
/// Перекладывание событий широковещателя в серверный поток gRPC.
/// </summary>
/// <remarks>
/// Живёт в gRPC-проекте, а не в
/// <see cref="MARS.Platform.Infrastructure.Events.EventBroadcaster{TMessage}"/>:
/// поток и его отмена — свойство транспорта, и широковещатель без gRPC обязан
/// собираться без <c>Grpc.Core</c>. Когда серверных потоков не останется, вместе
/// с ними уйдёт и это расширение.
/// </remarks>
public static class GrpcStreamPumpExtensions
{
    /// <summary>
    /// Отдаёт сообщения подписки в поток, пока подписка не закрыта или вызов не
    /// отменён.
    /// </summary>
    /// <returns><see langword="true"/>, если поток завершился сам.</returns>
    public static async Task<bool> PumpAsync<TMessage>(
        this EventSubscription<TMessage> subscription,
        IAsyncStreamWriter<TMessage> writer,
        CancellationToken cancellationToken
    )
        where TMessage : class
    {
        var stopped = false;

        while (!stopped && !cancellationToken.IsCancellationRequested)
        {
            stopped = await TryForwardNextAsync(subscription, writer, cancellationToken);
        }

        return stopped;
    }

    private static async Task<bool> TryForwardNextAsync<TMessage>(
        EventSubscription<TMessage> subscription,
        IAsyncStreamWriter<TMessage> writer,
        CancellationToken cancellationToken
    )
        where TMessage : class
    {
        var stopped = false;

        try
        {
            var message = await subscription.Reader.ReadAsync(cancellationToken);
            await writer.WriteAsync(message);
        }
        catch (OperationCanceledException)
        {
            stopped = true;
        }
        catch (ChannelClosedException)
        {
            stopped = true;
        }

        return stopped;
    }
}
