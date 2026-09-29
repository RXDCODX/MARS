namespace MARS.Shared.Messaging;

public interface IMarsEventBus
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default);
}
