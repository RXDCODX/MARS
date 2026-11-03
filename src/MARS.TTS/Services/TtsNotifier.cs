using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Voice;
using MARS.TTS.Grpc;
using MARS.TTS.Models;

namespace MARS.TTS.Services;

public sealed class TtsNotifier(
    GrpcEventBroadcaster<VoiceEvent> broadcaster,
    ILogger<TtsNotifier> logger,
    ITtsMessageFilterService ttsMessageFilterService
) : ITtsNotifier
{
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

            await broadcaster.BroadcastAsync(
                new VoiceEvent
                {
                    PlayTts = new PlayTtsEvent
                    {
                        User = TtsGrpcMapper.ToProto(ttsUser),
                        Message = message,
                    },
                }
            );

            logger.LogInformation(
                "TTS broadcast was sent to gRPC subscribers for user {User}",
                user.DisplayName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast TTS message to gRPC subscribers.");
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

            await broadcaster.BroadcastAsync(
                new VoiceEvent { UpdateTtsState = TtsGrpcMapper.ToProto(stateToBroadcast) }
            );

            logger.LogInformation(
                "TTS state update was sent to gRPC subscribers: {@State}",
                stateToBroadcast
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast TTS state to gRPC subscribers.");
        }
    }

    public async Task BroadcastReassignVoiceAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await broadcaster.BroadcastAsync(
                new VoiceEvent { ReassignVoice = new ReassignVoiceEvent { UserId = userId } }
            );

            logger.LogInformation(
                "Voice reassign broadcast sent to gRPC subscribers for user {UserId}",
                userId
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast voice reassign to gRPC subscribers.");
        }
    }
}
