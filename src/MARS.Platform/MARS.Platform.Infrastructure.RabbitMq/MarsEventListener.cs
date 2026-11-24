using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Infrastructure.RabbitMq;

/// <summary>
/// Типизированная базовая реализация Listener.
/// Конкретный обработчик наследуется от закрытого MarsEventListener&lt;TMessage&gt;,
/// поэтому тип сообщения задаётся один раз через контракт IMarsMessageListener<TMessage>.
/// </summary>
public abstract class MarsEventListener<TMessage>
    : IMarsEventListener<TMessage>,
        IMarsMessageListener<TMessage>,
        IMarsListener
    where TMessage : IMarsEventMessage
{
    public abstract Task Handle(TMessage message, CancellationToken cancellationToken);
}
