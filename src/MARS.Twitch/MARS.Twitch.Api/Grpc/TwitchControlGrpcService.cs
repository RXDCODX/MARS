using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Shared.Contracts.Clients;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.AutoMessages;
using MARS.Twitch.Infrastructure.Services.BlackList;
using MARS.Twitch.Infrastructure.Services.Connection;
using MARS.Twitch.Infrastructure.Services.EventSub;
using MARS.Twitch.Infrastructure.Services.HelloVideos;
using MARS.Twitch.Infrastructure.Services.Management;
using MARS.Twitch.Infrastructure.Services.MiniGamesStats;
using MARS.Twitch.Infrastructure.Services.Rewards;
using MARS.Twitch.Infrastructure.Services.PuntoSwitcher;
using MARS.Twitch.Infrastructure.Services.StreamManagement;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.Moderation;
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
    TwitchConnectionManager twitchConnectionManager,
    TwitchStreamManagementService streamManagementService,
    TwitchBlackListService blackListService,
    AutoMessagesHandler autoMessagesHandler,
    HelloVideoWorker helloVideoWorker,
    MiniGamesManager miniGamesManager,
    PuntoSwitcherService puntoSwitcherService,
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

    public override async Task<ProtoTwitch.StreamTitleResponse> GetStreamTitle(
        Empty request,
        ServerCallContext context
    )
    {
        var title = await streamManagementService.GetCurrentTitleAsync();
        return new ProtoTwitch.StreamTitleResponse { Title = title ?? string.Empty };
    }

    public override async Task<ProtoTwitch.BoolResult> ChangeStreamTitle(
        ProtoTwitch.ChangeStreamTitleRequest request,
        ServerCallContext context
    )
    {
        var title = (request?.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return new ProtoTwitch.BoolResult { Success = false };
        }

        if (title.Length > 140)
        {
            title = title[..140];
        }

        var success = await streamManagementService.ChangeStreamTitleAsync(title);
        return new ProtoTwitch.BoolResult { Success = success };
    }

    public override async Task<ProtoTwitch.TwitchUser> AddBlacklistedUser(
        ProtoTwitch.BlacklistUserRequest request,
        ServerCallContext context
    )
    {
        var user = await blackListService.AddTwitchBlacklistedUserAsync(request?.Input, true, context.CancellationToken);
        if (user is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Пользователь не найден"));
        }

        return new ProtoTwitch.TwitchUser
        {
            TwitchId = user.TwitchId,
            UserLogin = user.DisplayName ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
        };
    }

    public override async Task<ProtoTwitch.BoolResult> RemoveBlacklistedUser(
        ProtoTwitch.BlacklistUserRequest request,
        ServerCallContext context
    )
    {
        var user = await blackListService.RemoveTwitchBlacklistedUserAsync(request?.Input, context.CancellationToken);
        return new ProtoTwitch.BoolResult { Success = user is not null };
    }

    public override async Task<ProtoTwitch.BoolResult> Reconnect(
        Empty request,
        ServerCallContext context
    )
    {
        var success = await twitchConnectionManager.ReconnectAsync();
        return new ProtoTwitch.BoolResult { Success = success };
    }

    public override async Task<ProtoTwitch.StringResult> ExecuteAutoMessage(
        Empty request,
        ServerCallContext context
    )
    {
        await autoMessagesHandler.ExecuteAutoMessage();
        return new ProtoTwitch.StringResult { Text = "Авто-сообщение отправлено.", Success = true };
    }

    public override async Task<ProtoTwitch.StringResult> TestHelloVideo(
        ProtoTwitch.HelloVideoRequest request,
        ServerCallContext context
    )
    {
        var result = await helloVideoWorker.TestVideo(request?.Name ?? string.Empty, request?.Color);
        return new ProtoTwitch.StringResult { Text = result ?? "Не удалось сгенерировать видео.", Success = !string.IsNullOrWhiteSpace(result) };
    }

    public override async Task<ProtoTwitch.BoolResult> CancelMiniGames(
        Empty request,
        ServerCallContext context
    )
    {
        await miniGamesManager.CancelAllGamesAsync();
        return new ProtoTwitch.BoolResult { Success = true };
    }

    public override async Task<ProtoTwitch.BoolResult> TogglePuntoSwitcher(
        Empty request,
        ServerCallContext context
    )
    {
        var enabled = await puntoSwitcherService.ToggleFilterAsync(context.CancellationToken);
        return new ProtoTwitch.BoolResult { Success = enabled };
    }

    public override async Task<ProtoTwitch.StringResult> GetEventSubSubscriptions(
        Empty request,
        ServerCallContext context
    )
    {
        var response = await eventSubService.GetEventSubsAsync();
        if (response?.Data is null || response.Data.Count == 0)
        {
            return new ProtoTwitch.StringResult { Text = "Активных EventSub-подписок нет.", Success = true };
        }

        var lines = response.Data.Select(subscription => $"{subscription.Type} - {subscription.Status}");
        return new ProtoTwitch.StringResult { Text = string.Join("\n", lines), Success = true };
    }

    public override async Task<ProtoTwitch.StringResult> ResubscribeEventSub(
        Empty request,
        ServerCallContext context
    )
    {
        var result = await eventSubService.ResubscribeToEventSubAsync();
        return new ProtoTwitch.StringResult { Text = result, Success = true };
    }

    public override async Task<ProtoTwitch.BoolResult> TimeoutUser(
        ProtoTwitch.TimeoutUserRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request?.UserId))
        {
            return new ProtoTwitch.BoolResult { Success = false };
        }

        var accessToken = tokenService.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new ProtoTwitch.BoolResult { Success = false };
        }

        try
        {
            await api.Helix.Moderation.BanUserAsync(
                TwitchConstants.ChannelId,
                TwitchConstants.BotId,
                new BanUserRequest
                {
                    UserId = request.UserId,
                    DurationSeconds = 1,
                    Reason = "vanish",
                },
                accessToken
            );
            return new ProtoTwitch.BoolResult { Success = true };
        }
        catch
        {
            return new ProtoTwitch.BoolResult { Success = false };
        }
    }

    public override async Task<ProtoTwitch.BoolResult> JoinChannelAndSend(
        ProtoTwitch.JoinChannelAndSendRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request?.Channel) || string.IsNullOrWhiteSpace(request.Message))
        {
            return new ProtoTwitch.BoolResult { Success = false };
        }

        try
        {
            client.JoinChannel(request.Channel);
            await client.SendMessageAsync(request.Channel, request.Message);
            return new ProtoTwitch.BoolResult { Success = true };
        }
        catch
        {
            return new ProtoTwitch.BoolResult { Success = false };
        }
    }
}
