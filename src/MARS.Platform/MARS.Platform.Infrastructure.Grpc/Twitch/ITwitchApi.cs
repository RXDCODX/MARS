using MARS.Shared.Contracts.Twitch.Control;
using MARS.Shared.Contracts.Twitch.Rest;
using DomainTwitch = MARS.Shared.Contracts.Clients;

namespace MARS.Platform.Infrastructure.Grpc.Twitch;

public interface ITwitchApi
{
    Task<TwitchUserRestModel?> GetFullUserByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    );

    Task<bool> SendChatMessageAsync(
        TwitchChatSendRequest request,
        CancellationToken cancellationToken = default
    );

    Task<bool> GrantVipAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> RefundRewardAsync(
        string rewardId,
        IReadOnlyList<string> redemptionIds,
        string? reason,
        CancellationToken cancellationToken = default
    );

    Task<bool> RecordMiniGameResultAsync(
        string game,
        string userId,
        string userName,
        bool withWaifu,
        CancellationToken cancellationToken = default
    );

    Task<DomainTwitch.LeaderboardTop?> GetLeaderboardTopAsync(
        int count,
        CancellationToken cancellationToken = default
    );

    Task<DomainTwitch.LeaderboardStats?> GetUserStatsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    );

    Task<TwitchConnectionState> GetConnectionStateAsync(
        CancellationToken cancellationToken = default
    );
}
