using System.Threading;
using System.Threading.Tasks;
using MARS.Twitch.Models.Rest;

namespace MARS.Twitch.Models.Rest;

public interface ITwitchUsersClient
{
    Task<TwitchUserRestModel?> GetFullUserByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    );
}
