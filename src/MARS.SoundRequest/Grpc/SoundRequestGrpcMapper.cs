using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Entities;
using ProtoPlaybackState = MARS.Shared.Grpc.SoundRequest.SoundRequestPlaybackState;
using ProtoPlayerState = MARS.Shared.Grpc.SoundRequest.PlayerStateSnapshot;
using ProtoQueueItem = MARS.Shared.Grpc.SoundRequest.QueueItemSnapshot;
using ProtoTrackInfo = MARS.Shared.Grpc.SoundRequest.TrackInfo;
using ProtoVideoDisplay = MARS.Shared.Grpc.SoundRequest.SoundRequestVideoDisplay;

namespace MARS.SoundRequest.Grpc;

/// <summary>
/// Перевод доменных сущностей очереди звуковых запросов в proto.
/// </summary>
/// <remarks>
/// <para>
/// Только в одну сторону: обратное преобразование живёт в
/// <c>SoundRequestHubMapper</c> и отдаёт форму хаба, потому что именно её ждёт
/// браузер. Держать тут ещё и <c>ToDto</c> было бессмысленно — после переноса
/// команд клиента в <c>ISoundRequestPlayback</c> обратный путь по proto больше
/// не используется никем, то есть это были бы пять непокрытых методов без
/// единого вызова.
/// </para>
/// </remarks>
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

    public static ProtoVideoDisplay ToProto(VideoDisplay display)
    {
        return display switch
        {
            VideoDisplay.NoVideo => ProtoVideoDisplay.NoVideo,
            VideoDisplay.AudioOnly => ProtoVideoDisplay.AudioOnly,
            _ => ProtoVideoDisplay.Video,
        };
    }
}
