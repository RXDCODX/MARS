using MARS.Shared.Contracts.Voice;
using MARS.TTS.Application.Models;

namespace MARS.TTS.Application.Mapping;

public static class TtsPayloadMapper
{
    public static TtsUser ToPayload(TwitchUser user)
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

    public static TtsStateUpdate ToPayload(TtsState state)
    {
        return new TtsStateUpdate { IsStopped = state.IsStopped, Volume = state.Volume };
    }
}
