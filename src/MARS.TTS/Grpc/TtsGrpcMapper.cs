using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Voice;
using MARS.TTS.Models;

namespace MARS.TTS.Grpc;

public static class TtsGrpcMapper
{
    public static TtsUser ToProto(TwitchUser user)
    {
        return new TtsUser
        {
            TwitchId = user.TwitchId,
            UserLogin = user.UserLogin,
            DisplayName = user.DisplayName,
            ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
            ChatColor = user.ChatColor ?? string.Empty,
            IsModerator = user.IsModerator,
            IsVip = user.IsVip,
            HasFollowedAt = user.FollowedAt.HasValue,
            FollowedAt = user.FollowedAt?.ToString("O") ?? string.Empty,
            LastUpdated = user.LastUpdated.ToString("O"),
            CreatedAt = user.CreatedAt.ToString("O"),
            AliasNickname = user.AliasNickname ?? string.Empty,
            IsInBlockList = user.IsInBlockList,
        };
    }

    public static TtsStateUpdate ToProto(TtsState state)
    {
        return new TtsStateUpdate { IsStopped = state.IsStopped, Volume = state.Volume };
    }
}
