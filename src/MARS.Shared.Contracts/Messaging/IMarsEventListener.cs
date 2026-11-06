namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Маркер обработчика для явного discovery Wolverine.
/// </summary>
public interface IMarsEventListener;

/// <summary>
/// Типизированная базовая реализация Listener.
/// Конкретный обработчик наследуется от закрытого MarsEventListener&lt;TMessage&gt;,
/// поэтому тип сообщения задаётся один раз и доступен инфраструктуре через BaseType.
/// </summary>
public abstract class MarsEventListener<TMessage> : IMarsEventListener
    where TMessage : IMarsEventMessage
{
    public abstract Task Handle(TMessage message, CancellationToken cancellationToken);
}
