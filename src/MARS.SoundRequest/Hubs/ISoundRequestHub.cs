using MARS.SoundRequest.Entities;

namespace MARS.SoundRequest.Hubs;

public interface ISoundRequestHub
{
    Task PlayerStateChange(PlayerState playerState);
    Task QueueChanged(List<QueueItem> queue);
}
