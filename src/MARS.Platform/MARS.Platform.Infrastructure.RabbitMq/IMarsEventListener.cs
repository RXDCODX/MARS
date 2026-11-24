using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Infrastructure.RabbitMq;

/// <summary>
/// Маркер обработчика для явного discovery Wolverine.
/// </summary>
public interface IMarsEventListener<in TMessage>
    where TMessage : IMarsEventMessage
{
    public Task Handle(TMessage message, CancellationToken cancellationToken);
}
