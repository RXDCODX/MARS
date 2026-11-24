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
