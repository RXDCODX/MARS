using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class PostTwitchInfoEvent
{
    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
}
