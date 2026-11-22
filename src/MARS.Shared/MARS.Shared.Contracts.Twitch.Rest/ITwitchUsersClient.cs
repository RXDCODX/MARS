using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Twitch.Rest;

namespace MARS.Shared.Contracts.Twitch.Rest;

public interface ITwitchUsersClient
{
    Task<TwitchUserRestModel?> GetFullUserByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    );
}
