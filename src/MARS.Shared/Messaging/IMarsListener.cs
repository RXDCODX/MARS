namespace MARS.Shared.Messaging;

/// <summary>
/// Маркер обработчика для явного discovery Wolverine.
/// </summary>
public interface IMarsListener { }

/// <summary>
/// Типизированная базовая реализация Listener.
/// Конкретный обработчик наследуется от закрытого MarsListener&lt;TMessage&gt;,
/// поэтому тип сообщения задаётся один раз и доступен инфраструктуре через BaseType.
/// </summary>
public abstract class MarsListener<TMessage> : IMarsListener
    where TMessage : IMarsMessage
{
    public abstract Task Handle(TMessage message, CancellationToken cancellationToken);
}
