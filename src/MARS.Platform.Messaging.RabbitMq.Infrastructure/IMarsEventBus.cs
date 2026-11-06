namespace MARS.Platform.Messaging.RabbitMq.Infrastructure;

public interface IMarsEventBus
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default);
}
