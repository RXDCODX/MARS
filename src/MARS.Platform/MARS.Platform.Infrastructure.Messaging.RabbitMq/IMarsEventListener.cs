using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Маркер обработчика для явного discovery Wolverine.
/// </summary>
public interface IMarsEventListener<in TMessage>
    where TMessage : IMarsEventMessage
{
    public Task Handle(TMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Типизированная базовая реализация Listener.
/// Конкретный обработчик наследуется от закрытого MarsEventListener&lt;TMessage&gt;,
/// поэтому тип сообщения задаётся один раз через контракт IMarsMessageListener<TMessage>.
/// </summary>
public abstract class MarsEventListener<TMessage>
    : IMarsEventListener<TMessage>,
        IMarsMessageListener<TMessage>
    where TMessage : IMarsEventMessage
{
    public abstract Task Handle(TMessage message, CancellationToken cancellationToken);
}
