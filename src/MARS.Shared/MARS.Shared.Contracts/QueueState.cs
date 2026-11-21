namespace MARS.Shared.Contracts.SoundRequest;

public class QueueState
{
    public List<QueueItemSnapshot> Queue { get; set; } = new();
}
