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

    Task<string?> GetStreamTitleAsync(CancellationToken cancellationToken = default);

    Task<bool> ChangeStreamTitleAsync(string title, CancellationToken cancellationToken = default);

    Task<bool> AddBlacklistedUserAsync(string input, CancellationToken cancellationToken = default);

    Task<bool> RemoveBlacklistedUserAsync(string input, CancellationToken cancellationToken = default);

    Task<bool> ReconnectAsync(CancellationToken cancellationToken = default);

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

    Task ExecuteAutoMessageAsync(CancellationToken cancellationToken = default);

    Task<string?> TestHelloVideoAsync(string name, string? color, CancellationToken cancellationToken = default);

    Task CancelMiniGamesAsync(CancellationToken cancellationToken = default);

    Task<bool> TogglePuntoSwitcherAsync(CancellationToken cancellationToken = default);

    Task<string> GetEventSubSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task<string> ResubscribeEventSubAsync(CancellationToken cancellationToken = default);

    Task<bool> TimeoutUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> JoinChannelAndSendAsync(string channel, string message, CancellationToken cancellationToken = default);

    Task<bool> NotifyMissingTwitchTokenAsync(CancellationToken cancellationToken = default);
}
