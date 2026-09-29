using MARS.TTS.Hubs;
using MARS.TTS.Hubs.Interfaces;
using MARS.TTS.Hubs.Models;
using MARS.TTS.Models;
using Microsoft.AspNetCore.SignalR;

namespace MARS.TTS.Services;

public class TtsHubBroadcaster(
    IHubContext<VoiceRecognitionHub, IVoiceRecognitionHub> hubContext,
    ILogger<TtsHubBroadcaster> logger,
    ITtsMessageFilterService ttsMessageFilterService
) : ITtsHubBroadcaster
{
    private const string TtsConsumersGroupName = "tts-consumers";
    private readonly Lock _stateGate = new();

    public double CurrentVolume
    {
        get
        {
            lock (_stateGate)
            {
                return field;
            }
        }
        private set;
    } = 1.0;

    public async Task BroadcastAsync(
        TwitchUser? user,
        string message,
        CancellationToken cancellationToken = default
    )
    {
        if (user is null || string.IsNullOrWhiteSpace(message))
        {
            logger.LogWarning("TTS broadcast was skipped because the user or message is empty.");
            return;
        }

        if (ttsMessageFilterService.IsFilterEnabled)
        {
            var filterResult = ttsMessageFilterService.FilterMessage(message, user.TwitchId);

            if (!filterResult.Success)
            {
                logger.LogInformation(
                    "TTS broadcast was skipped by filter for user {User}: {Reason}",
                    user.DisplayName,
                    filterResult.ErrorMessage
                );
                return;
            }

            message = filterResult.Result!;
        }

        try
        {
            var ttsUser = user;

            if (!string.IsNullOrWhiteSpace(user.AliasNickname))
            {
                ttsUser = new TwitchUser
                {
                    TwitchId = user.TwitchId,
                    UserLogin = user.UserLogin,
                    DisplayName = user.AliasNickname,
                    ProfileImageUrl = user.ProfileImageUrl,
                    ChatColor = user.ChatColor,
                    IsModerator = user.IsModerator,
                    IsVip = user.IsVip,
                    FollowedAt = user.FollowedAt,
                    LastUpdated = user.LastUpdated,
                    CreatedAt = user.CreatedAt,
                    IsInBlockList = user.IsInBlockList,
                };
            }

            await hubContext.Clients.Group(TtsConsumersGroupName).PlayTts(ttsUser, message);
            logger.LogInformation(
                "TTS broadcast was sent to hub consumers for user {User}",
                user.DisplayName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast TTS message to hub consumers.");
        }
    }

    public async Task BroadcastStateAsync(
        TtsState? state,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (state is not null)
            {
                lock (_stateGate)
                {
                    CurrentVolume = Math.Clamp(state.Volume, 0.0, 2.0);
                }
            }

            var stateToBroadcast = state ?? new TtsState { Volume = CurrentVolume };

            await hubContext.Clients.Group(TtsConsumersGroupName).UpdateTtsState(stateToBroadcast);
            logger.LogInformation(
                "TTS state update was sent to hub consumers: {@State}",
                stateToBroadcast
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast TTS state to hub consumers.");
        }
    }

    public async Task BroadcastReassignVoiceAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await hubContext.Clients.Group(TtsConsumersGroupName).ReassignVoice(userId);

            logger.LogInformation(
                "Voice reassign broadcast sent to hub consumers for user {UserId}",
                userId
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast voice reassign to hub consumers.");
        }
    }
}
