using MARS.Shared.Contracts.Messaging;
using Wolverine;
using Wolverine.Runtime;

namespace MARS.Platform.Messaging.RabbitMq.Infrastructure;

/// <summary>
/// Адаптер typed publishing для singleton и hosted services.
/// Wolverine IMessageBus scoped, поэтому используем runtime и создаём MessageBus
/// на время публикации, не захватывая scoped dependency.
/// </summary>
public sealed class WolverineMarsEventBus(IWolverineRuntime runtime) : IMarsWolverineEventBus
{
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class, IMarsEventMessage
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();

        var bus = new MessageBus(runtime);
        await bus.PublishAsync(message).ConfigureAwait(false);
    }
}

/// <summary>Заглушка для сервисов/сред, где RabbitMQ не настроен.</summary>
internal sealed class NullMarsWolverineEventBus : IMarsWolverineEventBus
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class, IMarsEventMessage
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
