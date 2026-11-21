using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class NewMessageEvent
{
    public string Id { get; set; } = "";
    public string MessageJson { get; set; } = "";
}
