using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Entities;
using ProtoPlaybackState = MARS.Shared.Grpc.SoundRequest.SoundRequestPlaybackState;
using ProtoPlayerState = MARS.Shared.Grpc.SoundRequest.PlayerStateSnapshot;
using ProtoQueueItem = MARS.Shared.Grpc.SoundRequest.QueueItemSnapshot;
using ProtoTrackInfo = MARS.Shared.Grpc.SoundRequest.TrackInfo;
using ProtoVideoDisplay = MARS.Shared.Grpc.SoundRequest.SoundRequestVideoDisplay;

namespace MARS.SoundRequest.Grpc;

public static class SoundRequestGrpcMapper
{
    public static ProtoPlayerState ToProto(PlayerState state)
    {
        return new ProtoPlayerState
        {
            Id = state.Id.ToString(),
            CurrentQueueItemId = state.CurrentQueueItemId?.ToString() ?? string.Empty,
            HasCurrentTrackProgress = state.CurrentTrackProgress.HasValue,
            CurrentTrackProgressSeconds = (long)(state.CurrentTrackProgress?.TotalSeconds ?? 0),
            State = ToProto(state.State),
            VideoState = ToProto(state.VideoState),
            IsMuted = state.IsMuted,
            PausedByMute = state.PausedByMute,
            Volume = state.Volume,
            CurrentQueueItem = ToProto(state.CurrentQueueItem),
        };
    }

    public static PlayerState ToDto(ProtoPlayerState state)
    {
        return new PlayerState
        {
            Id = MediaGrpcMapper.ParseGuid(state.Id),
            CurrentQueueItemId = string.IsNullOrEmpty(state.CurrentQueueItemId)
                ? null
                : MediaGrpcMapper.ParseGuid(state.CurrentQueueItemId),
            CurrentTrackProgress = state.HasCurrentTrackProgress
                ? TimeSpan.FromSeconds(state.CurrentTrackProgressSeconds)
                : null,
            State = ToDto(state.State),
            VideoState = ToDto(state.VideoState),
            IsMuted = state.IsMuted,
            PausedByMute = state.PausedByMute,
            Volume = state.Volume,
            CurrentQueueItem = ToDto(state.CurrentQueueItem),
        };
    }

    public static List<ProtoQueueItem> ToProto(List<QueueItem> queue)
    {
        return [.. queue.Select(ToProto)];
    }

    public static ProtoQueueItem ToProto(QueueItem? item)
    {
        return new ProtoQueueItem
        {
            Id = item?.Id.ToString() ?? string.Empty,
            TrackId = item?.TrackId.ToString() ?? string.Empty,
            Track = ToProto(item?.Track),
            QueueOrder = item?.QueueOrder ?? 0,
            RequestedByTwitchId = item?.RequestedByTwitchId ?? string.Empty,
            RequestedAt = item?.RequestedAt.ToString("O") ?? string.Empty,
        };
    }

    public static QueueItem? ToDto(ProtoQueueItem? item)
    {
        if (item is null)
        {
            return null;
        }

        return new QueueItem
        {
            Id = MediaGrpcMapper.ParseGuid(item.Id),
            TrackId = MediaGrpcMapper.ParseGuid(item.TrackId),
            Track = ToDto(item.Track),
            QueueOrder = item.QueueOrder,
            RequestedByTwitchId = item.RequestedByTwitchId,
            RequestedAt = MediaGrpcMapper.ParseDateTime(item.RequestedAt),
        };
    }

    public static ProtoTrackInfo ToProto(BaseTrackInfo? track)
    {
        var info = new ProtoTrackInfo
        {
            Id = track?.Id.ToString() ?? string.Empty,
            TrackName = track?.TrackName ?? string.Empty,
            DurationSeconds = (long)(track?.Duration.TotalSeconds ?? 0),
            Url = track?.Url.ToString() ?? string.Empty,
            LastTimePlays = track?.LastTimePlays.ToString("O") ?? string.Empty,
            ArtworkUrl = track?.ArtworkUrl?.ToString() ?? string.Empty,
            VideoId = track?.VideoId ?? string.Empty,
            IsDeleted = track?.IsDeleted ?? false,
            CreatedAt = track?.CreatedAt.ToString("O") ?? string.Empty,
            UpdatedAt = track?.UpdatedAt.ToString("O") ?? string.Empty,
        };

        info.Authors.AddRange(track?.Authors ?? []);

        return info;
    }

    public static BaseTrackInfo? ToDto(ProtoTrackInfo? track)
    {
        if (track is null)
        {
            return null;
        }

        return new BaseTrackInfo
        {
            Id = MediaGrpcMapper.ParseGuid(track.Id),
            TrackName = track.TrackName,
            Authors = [.. track.Authors],
            Duration = TimeSpan.FromSeconds(track.DurationSeconds),
            Url = new Uri(track.Url),
            LastTimePlays = MediaGrpcMapper.ParseDateTime(track.LastTimePlays),
            ArtworkUrl = string.IsNullOrEmpty(track.ArtworkUrl) ? null : new Uri(track.ArtworkUrl),
            VideoId = track.VideoId,
            IsDeleted = track.IsDeleted,
            CreatedAt = MediaGrpcMapper.ParseDateTime(track.CreatedAt),
            UpdatedAt = MediaGrpcMapper.ParseDateTime(track.UpdatedAt),
        };
    }

    public static ProtoPlaybackState ToProto(PlaybackState state)
    {
        return state switch
        {
            PlaybackState.Playing => ProtoPlaybackState.Playing,
            PlaybackState.Paused => ProtoPlaybackState.Paused,
            PlaybackState.SwitchingTrack => ProtoPlaybackState.SwitchingTrack,
            PlaybackState.WaitingForTrack => ProtoPlaybackState.WaitingForTrack,
            _ => ProtoPlaybackState.Stopped,
        };
    }

    public static PlaybackState ToDto(ProtoPlaybackState state)
    {
        return state switch
        {
            ProtoPlaybackState.Playing => PlaybackState.Playing,
            ProtoPlaybackState.Paused => PlaybackState.Paused,
            ProtoPlaybackState.SwitchingTrack => PlaybackState.SwitchingTrack,
            ProtoPlaybackState.WaitingForTrack => PlaybackState.WaitingForTrack,
            _ => PlaybackState.Stopped,
        };
    }

    public static ProtoVideoDisplay ToProto(VideoDisplay display)
    {
        return display switch
        {
            VideoDisplay.NoVideo => ProtoVideoDisplay.NoVideo,
            VideoDisplay.AudioOnly => ProtoVideoDisplay.AudioOnly,
            _ => ProtoVideoDisplay.Video,
        };
    }

    public static VideoDisplay ToDto(ProtoVideoDisplay display)
    {
        return display switch
        {
            ProtoVideoDisplay.NoVideo => VideoDisplay.NoVideo,
            ProtoVideoDisplay.AudioOnly => VideoDisplay.AudioOnly,
            _ => VideoDisplay.Video,
        };
    }
}
