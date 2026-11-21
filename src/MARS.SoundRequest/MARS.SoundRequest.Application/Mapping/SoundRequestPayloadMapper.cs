using MARS.Shared.Contracts.SoundRequest;
using MARS.SoundRequest.Application.Models;
using MARS.SoundRequest.Domain.Entities;
using ProtoPlaybackState = MARS.Shared.Contracts.SoundRequest.SoundRequestPlaybackState;
using ProtoPlayerState = MARS.Shared.Contracts.SoundRequest.PlayerStateSnapshot;
using ProtoQueueItem = MARS.Shared.Contracts.SoundRequest.QueueItemSnapshot;
using ProtoTrackInfo = MARS.Shared.Contracts.SoundRequest.TrackInfo;
using ProtoVideoDisplay = MARS.Shared.Contracts.SoundRequest.SoundRequestVideoDisplay;

namespace MARS.SoundRequest.Application.Mapping;

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
public static class SoundRequestPayloadMapper
{
    public static ProtoPlayerState ToPayload(PlayerState state)
    {
        return new ProtoPlayerState
        {
            Id = state.Id.ToString(),
            CurrentQueueItemId = state.CurrentQueueItemId?.ToString() ?? string.Empty,
            HasCurrentTrackProgress = state.CurrentTrackProgress.HasValue,
            CurrentTrackProgressSeconds = (long)(state.CurrentTrackProgress?.TotalSeconds ?? 0),
            State = ToPayload(state.State),
            VideoState = ToPayload(state.VideoState),
            IsMuted = state.IsMuted,
            PausedByMute = state.PausedByMute,
            Volume = state.Volume,
            CurrentQueueItem = ToPayload(state.CurrentQueueItem),
        };
    }

    public static List<ProtoQueueItem> ToPayload(List<QueueItem> queue)
    {
        return [.. queue.Select(ToPayload)];
    }

    public static ProtoQueueItem ToPayload(QueueItem? item)
    {
        return new ProtoQueueItem
        {
            Id = item?.Id.ToString() ?? string.Empty,
            TrackId = item?.TrackId.ToString() ?? string.Empty,
            Track = ToPayload(item?.Track),
            QueueOrder = item?.QueueOrder ?? 0,
            RequestedByTwitchId = item?.RequestedByTwitchId ?? string.Empty,
            RequestedAt = item?.RequestedAt.ToString("O") ?? string.Empty,
        };
    }

    public static ProtoTrackInfo ToPayload(BaseTrackInfo? track)
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

    public static ProtoPlaybackState ToPayload(PlaybackState state)
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

    public static ProtoVideoDisplay ToPayload(VideoDisplay display)
    {
        return display switch
        {
            VideoDisplay.NoVideo => ProtoVideoDisplay.NoVideo,
            VideoDisplay.AudioOnly => ProtoVideoDisplay.AudioOnly,
            _ => ProtoVideoDisplay.Video,
        };
    }
}
