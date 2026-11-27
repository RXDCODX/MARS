using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.Twitch.Rest;
using DomainTwitch = MARS.Shared.Contracts.Clients;
using ProtoTwitch = MARS.Shared.Contracts.Twitch.Control;

namespace MARS.Platform.Infrastructure.Grpc.Twitch;

internal sealed class TwitchApi(GrpcClientFactory clientFactory) : ITwitchApi
{
    public async Task<MARS.Shared.Contracts.Twitch.Rest.TwitchUserRestModel?> GetFullUserByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        try
        {
            var response = await clientFactory
                .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>(
                    "TwitchCore"
                )
                .GetFullUserByLoginAsync(
                    new ProtoTwitch.GetUserByLoginRequest { Login = login },
                    cancellationToken: cancellationToken
                );

            return new MARS.Shared.Contracts.Twitch.Rest.TwitchUserRestModel
            {
                TwitchId = response.TwitchId,
                UserLogin = response.UserLogin,
                DisplayName = response.DisplayName,
                ProfileImageUrl = string.IsNullOrWhiteSpace(response.ProfileImageUrl)
                    ? null
                    : response.ProfileImageUrl,
                ChatColor = string.IsNullOrWhiteSpace(response.ChatColor)
                    ? null
                    : response.ChatColor,
                IsModerator = response.IsModerator,
                IsVip = response.IsVip,
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> SendChatMessageAsync(
        ProtoTwitch.TwitchChatSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>("TwitchCore")
            .SendChatMessageAsync(
                new ProtoTwitch.TwitchChatSendRequest
                {
                    Channel = request.Channel,
                    Message = request.Message,
                    ReplyTo = request.ReplyTo ?? string.Empty,
                },
                cancellationToken: cancellationToken
            );

        return response.Success;
    }

    public async Task<bool> GrantVipAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>("TwitchCore")
            .GrantVipAsync(
                new ProtoTwitch.TwitchVipGrantRequest { UserId = userId },
                cancellationToken: cancellationToken
            );

        return response.Success;
    }

    public async Task<bool> RefundRewardAsync(
        string rewardId,
        IReadOnlyList<string> redemptionIds,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        var request = new ProtoTwitch.RewardRefundRequest
        {
            RewardId = rewardId,
            Reason = reason ?? string.Empty,
        };
        request.RedemptionIds.AddRange(redemptionIds);

        var response = await clientFactory
            .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>("TwitchCore")
            .RefundRewardAsync(request, cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> RecordMiniGameResultAsync(
        string game,
        string userId,
        string userName,
        bool withWaifu,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>("TwitchCore")
            .RecordMiniGameResultAsync(
                new ProtoTwitch.MiniGameResultRequest
                {
                    Game = game,
                    UserId = userId,
                    UserName = userName,
                    WithWaifu = withWaifu,
                },
                cancellationToken: cancellationToken
            );

        return response.Success;
    }

    public async Task<DomainTwitch.LeaderboardTop?> GetLeaderboardTopAsync(
        int count,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>("TwitchCore")
            .GetLeaderboardTopAsync(
                new ProtoTwitch.GetLeaderboardTopRequest { Count = count },
                cancellationToken: cancellationToken
            );

        return new DomainTwitch.LeaderboardTop(response.Entries.Select(ToModel).ToList());
    }

    public async Task<DomainTwitch.LeaderboardStats?> GetUserStatsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>("TwitchCore")
            .GetUserStatsAsync(
                new ProtoTwitch.GetUserStatsRequest { TwitchId = twitchId },
                cancellationToken: cancellationToken
            );

        return new DomainTwitch.LeaderboardStats(
            response.Place,
            response.HasUser ? ToModel(response.User) : null
        );
    }

    public async Task<TwitchConnectionState> GetConnectionStateAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<ProtoTwitch.TwitchControlService.TwitchControlServiceClient>("TwitchCore")
            .GetConnectionStateAsync(new Empty(), cancellationToken: cancellationToken);

        return new TwitchConnectionState(
            response.IsEventSubConnected,
            response.IsTwitchChatConnected
        );
    }

    private static DomainTwitch.LeaderboardEntry ToModel(ProtoTwitch.LeaderboardEntry proto) =>
        new(
            proto.TwitchId,
            string.IsNullOrWhiteSpace(proto.DisplayName) ? null : proto.DisplayName,
            proto.TotalWins,
            proto.RussianRouletteWins,
            proto.TriviaWins
        );
}
