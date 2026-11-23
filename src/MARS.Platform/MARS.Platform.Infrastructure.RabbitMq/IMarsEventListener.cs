using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Infrastructure.RabbitMq;

/// <summary>
/// Неродовой маркер слушателя для discovery Wolverine. Не зависит от вариативности
/// <see cref="IMarsEventListener{TMessage}"/> и гарантирует совпадение в фильтре
/// <c>Implements&lt;T&gt;()</c> для любого конкретного <see cref="MarsEventListener{TMessage}"/>.
/// </summary>
public interface IMarsListener { }

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
        IMarsMessageListener<TMessage>,
        IMarsListener
    where TMessage : IMarsEventMessage
{
    public abstract Task Handle(TMessage message, CancellationToken cancellationToken);
}
