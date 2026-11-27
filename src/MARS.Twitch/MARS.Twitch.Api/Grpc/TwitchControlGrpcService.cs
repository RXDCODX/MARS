using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Shared.Contracts.Clients;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.Connection;
using MARS.Twitch.Infrastructure.Services.EventSub;
using MARS.Twitch.Infrastructure.Services.Management;
using MARS.Twitch.Infrastructure.Services.MiniGamesStats;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;
using ProtoTwitch = MARS.Shared.Contracts.Twitch.Control;

namespace MARS.Twitch.Api.Grpc;

public sealed class TwitchControlGrpcService(
    ITwitchAPI api,
    ITwitchClient client,
    TokenService tokenService,
    ILeaderboardService leaderboardService,
    EventSubService eventSubService,
    ITwitchConnectionState twitchConnectionManager,
    ILogger<TwitchControlGrpcService> logger
) : ProtoTwitch.TwitchControlService.TwitchControlServiceBase
{
    private const int MaxMessageLength = 500;

    public override async Task<ProtoTwitch.TwitchUser> GetFullUserByLogin(
        ProtoTwitch.GetUserByLoginRequest request,
        ServerCallContext context
    )
    {
        var normalized = (request.Login ?? string.Empty).Trim().TrimStart('@');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Логин не передан"));
        }

        var response = await api.Helix.Users.GetUsersAsync(null, new List<string> { normalized });
        var user = response?.Users?.FirstOrDefault();
        if (user is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Пользователь не найден"));
        }

        return new ProtoTwitch.TwitchUser
        {
            TwitchId = user.Id,
            UserLogin = user.Login,
            DisplayName = user.DisplayName,
            ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
            ChatColor = string.Empty,
            IsModerator = false,
            IsVip = false,
        };
    }

    public override async Task<ProtoTwitch.BoolResult> SendChatMessage(
        ProtoTwitch.TwitchChatSendRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return new ProtoTwitch.BoolResult { Success = true };
        }

        var channel = string.IsNullOrWhiteSpace(request.Channel)
            ? TwitchConstants.Channel
            : request.Channel.Trim();
        var normalized = request
            .Message.Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("  ", " ")
            .Trim();

        while (normalized.Length > MaxMessageLength)
        {
            var splitAt = normalized.LastIndexOf(' ', MaxMessageLength);
            if (splitAt <= 0)
            {
                splitAt = MaxMessageLength;
            }

            await client.SendMessageAsync(channel, normalized[..splitAt]);
            normalized = normalized[splitAt..].TrimStart(' ');
        }

        if (normalized.Length > 0)
        {
            await client.SendMessageAsync(channel, normalized);
        }

        return new ProtoTwitch.BoolResult { Success = true };
    }

    public override async Task<ProtoTwitch.BoolResult> GrantVip(
        ProtoTwitch.TwitchVipGrantRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UserId))
        {
            return new ProtoTwitch.BoolResult { Success = true };
        }

        var accessToken = tokenService.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("VIP grant: no access token available");
            return new ProtoTwitch.BoolResult { Success = false };
        }

        try
        {
            await api.Helix.Channels.AddChannelVIPAsync(
                TwitchConstants.ChannelId,
                request.UserId,
                accessToken
            );
            logger.LogInformation("VIP granted to user {UserId}", request.UserId);
            return new ProtoTwitch.BoolResult { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Twitch operation failed");
            return new ProtoTwitch.BoolResult { Success = false };
        }
    }

    public override async Task<ProtoTwitch.BoolResult> RefundReward(
        ProtoTwitch.RewardRefundRequest request,
        ServerCallContext context
    )
    {
        if (
            request is null
            || string.IsNullOrWhiteSpace(request.RewardId)
            || request.RedemptionIds.Count == 0
        )
        {
            logger.LogWarning("Reward refund has no reward/redemption ids, skipped");
            return new ProtoTwitch.BoolResult { Success = true };
        }

        var accessToken = tokenService.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("Reward refund: no access token available");
            return new ProtoTwitch.BoolResult { Success = false };
        }

        try
        {
            await api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                TwitchConstants.ChannelId,
                request.RewardId,
                request.RedemptionIds.ToList(),
                new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus.UpdateCustomRewardRedemptionStatusRequest
                {
                    Status = TwitchLib.Api.Core.Enums.CustomRewardRedemptionStatus.CANCELED,
                },
                accessToken
            );

            logger.LogInformation(
                "Refunded {Count} redemptions of reward {RewardId}: {Reason}",
                request.RedemptionIds.Count,
                request.RewardId,
                request.Reason
            );
            return new ProtoTwitch.BoolResult { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Twitch operation failed");
            return new ProtoTwitch.BoolResult { Success = false };
        }
    }

    public override async Task<ProtoTwitch.BoolResult> RecordMiniGameResult(
        ProtoTwitch.MiniGameResultRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UserId))
        {
            logger.LogWarning("MiniGame result has no user id, skipped");
            return new ProtoTwitch.BoolResult { Success = true };
        }

        try
        {
            switch (request.Game)
            {
                case "russian_roulette":
                    await leaderboardService.RecordRouletteWinAsync(
                        request.UserId,
                        request.WithWaifu,
                        context.CancellationToken
                    );
                    break;
                case "quiz":
                    await leaderboardService.RecordTriviaWinAsync(
                        request.UserId,
                        request.WithWaifu,
                        context.CancellationToken
                    );
                    break;
                case "audio_quiz":
                    logger.LogInformation("AudioQuiz win by {UserName}", request.UserName);
                    break;
            }

            return new ProtoTwitch.BoolResult { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record mini-game result");
            return new ProtoTwitch.BoolResult { Success = false };
        }
    }

    public override async Task<ProtoTwitch.LeaderboardTopResponse> GetLeaderboardTop(
        ProtoTwitch.GetLeaderboardTopRequest request,
        ServerCallContext context
    )
    {
        var result = await leaderboardService.GetTopAsync(request.Count, context.CancellationToken);
        if (!result.Success || result.Result is null)
        {
            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    result.ErrorMessage ?? "Не удалось получить топ победителей мини-игр"
                )
            );
        }

        var response = new ProtoTwitch.LeaderboardTopResponse();
        response.Entries.AddRange(
            result.Result.Select(user => new ProtoTwitch.LeaderboardEntry
            {
                TwitchId = user.TwitchId,
                DisplayName = user.TwitchUser?.DisplayName ?? string.Empty,
                TotalWins = user.TotalWins,
                RussianRouletteWins = user.RussianRouletteWins,
                TriviaWins = user.TriviaWins,
            })
        );

        return response;
    }

    public override async Task<ProtoTwitch.LeaderboardStatsResponse> GetUserStats(
        ProtoTwitch.GetUserStatsRequest request,
        ServerCallContext context
    )
    {
        var result = await leaderboardService.GetUserStatsAsync(
            request.TwitchId,
            context.CancellationToken
        );
        if (!result.Success)
        {
            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    result.ErrorMessage ?? "Не удалось получить статистику мини-игр"
                )
            );
        }

        var stats = result.Result!;
        var response = new ProtoTwitch.LeaderboardStatsResponse
        {
            Place = stats.Place,
            HasUser = stats.User is not null,
        };

        if (stats.User is not null)
        {
            response.User = new ProtoTwitch.LeaderboardEntry
            {
                TwitchId = stats.User.TwitchId,
                DisplayName = stats.User.TwitchUser?.DisplayName ?? string.Empty,
                TotalWins = stats.User.TotalWins,
                RussianRouletteWins = stats.User.RussianRouletteWins,
                TriviaWins = stats.User.TriviaWins,
            };
        }

        return response;
    }

    public override Task<ProtoTwitch.TwitchConnectionState> GetConnectionState(
        Empty request,
        ServerCallContext context
    ) =>
        Task.FromResult(
            new ProtoTwitch.TwitchConnectionState
            {
                IsEventSubConnected = eventSubService.IsWebSocketConnected,
                IsTwitchChatConnected = twitchConnectionManager.IsConnected,
                  JoinedChannels = twitchConnectionManager.JoinedChannels,
                  LastConnectedAt = twitchConnectionManager.LastConnectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                  LastDisconnectedAt = twitchConnectionManager.LastDisconnectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                  LastDisconnectReason = twitchConnectionManager.LastDisconnectReason ?? string.Empty,
                  ReconnectAttempts = twitchConnectionManager.ReconnectAttempts,
                  IsReconnecting = twitchConnectionManager.IsReconnecting,
            }
        );
}
