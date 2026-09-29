using MARS.TwitchCore.Entities;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.TwitchCore.Services;

public interface ITwitchUserEnsureService
{
    Task<TwitchUser> EnsureUserExistsAsync(
        ChatMessage chatMessage,
        CancellationToken cancellationToken = default
    );

    Task<TwitchUser> EnsureUserExistsAsync(
        OnMessageReceivedArgs args,
        CancellationToken cancellationToken = default
    );

    Task<TwitchUser> EnsureUserExistsAsync(
        ChannelPointsCustomRewardRedemptionArgs args,
        CancellationToken cancellationToken = default
    );

    Task<TwitchUser> EnsureUserExistsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    );

    Task<TwitchUser> EnsureUserExistsAsync(
        TwitchUser? twitchUser,
        CancellationToken cancellationToken = default
    );

    Task<TwitchUser?> EnsureUserExistsByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    );
}
