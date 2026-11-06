using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Messaging.RabbitMq.Infrastructure;

/// <summary>
/// Маркер обработчика для явного discovery Wolverine.
/// </summary>
public interface IMarsEventListener;

/// <summary>
/// Типизированная базовая реализация Listener.
/// Конкретный обработчик наследуется от закрытого MarsEventListener&lt;TMessage&gt;,
/// поэтому тип сообщения задаётся один раз и доступен инфраструктуре через BaseType.
/// </summary>
public abstract class MarsEventListener<TMessage>
    : IMarsEventListener,
        IMarsMessageListener<TMessage>
    where TMessage : IMarsEventMessage
{
    public abstract Task Handle(TMessage message, CancellationToken cancellationToken);
}
