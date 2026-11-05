namespace MARS.Shared.Messaging;

public interface IMarsListener<in TMessage>
{
    Task Handle(TMessage message, CancellationToken cancellationToken);
}
