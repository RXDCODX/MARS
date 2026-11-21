namespace MARS.Shared.Contracts.SoundRequest;

public class QueueItemSnapshot
{
    public string Id { get; set; } = "";
    public string TrackId { get; set; } = "";
    public TrackInfo? Track { get; set; }
    public int QueueOrder { get; set; }
    public string RequestedByTwitchId { get; set; } = "";
    public string RequestedAt { get; set; } = "";
}
