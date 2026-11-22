namespace MARS.Shared.Contracts.SoundRequest;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.SoundRequest;

public class SoundRequestEvent : OverlayEventAbstract
{
    public PlayerStateSnapshot? PlayerStateChange { get; set; }
    public QueueState? QueueChanged { get; set; }

    public enum EventOneofCase
    {
        None = 0,
        PlayerStateChange = 1,
        QueueChanged = 2,
    }

    public EventOneofCase EventCase
    {
        get
        {
            if (PlayerStateChange is not null)
                return EventOneofCase.PlayerStateChange;
            if (QueueChanged is not null)
                return EventOneofCase.QueueChanged;
            return EventOneofCase.None;
        }
    }
}
