using MARS.Shared.Contracts.Messaging;

namespace MARS.Platform.Messaging.RabbitMq.Infrastructure;

/// <summary>
/// Типизированная публикация событий через Wolverine.
/// В отличие от legacy IMarsEventBus, здесь не передаются routing keys:
/// адрес обмена определяется инфраструктурой по типу контракта.
/// </summary>
public interface IMarsWolverineEventBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class, IMarsEventMessage;
}
