using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts;

public class MikuMikuBeamEvent
{
    public List<TwitchUser> Users { get; set; } = new();
}
