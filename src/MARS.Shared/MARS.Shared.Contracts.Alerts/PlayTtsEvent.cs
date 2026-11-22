namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Voice;
using MARS.Shared.Kernel;

public class PlayTtsEvent : OverlayEventAbstract
{
    public TtsUser? User { get; set; }
    public string Message { get; set; } = "";
}
