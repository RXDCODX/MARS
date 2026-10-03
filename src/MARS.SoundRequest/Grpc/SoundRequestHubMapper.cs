using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Hubs.Dtos;
using ProtoPlaybackState = MARS.Shared.Grpc.SoundRequest.SoundRequestPlaybackState;
using ProtoVideoDisplay = MARS.Shared.Grpc.SoundRequest.SoundRequestVideoDisplay;

namespace MARS.SoundRequest.Grpc;

/// <summary>
/// Перевод между снимками из proto и формой хаба.
/// </summary>
/// <remarks>
/// <para>
/// Нужен потому, что одна команда клиента приходит двумя транспортами в двух
/// формах: gRPC-сервис получает снимок из proto, хаб — объект
/// <c>PlayerStateHubDto</c>, каким его шлёт браузер.
/// </para>
/// <para>
/// Перечисления сопоставляются поимени, а не по номеру: значение на проводе
/// приходит строкой, и молчаливое сравнение с числом дало бы состояние не
/// «играет».
/// </para>
/// </remarks>
public static class SoundRequestHubMapper
{
    /// <summary>Состояние из proto в форму хаба.</summary>
    public static PlayerStateHubDto ToHubState(PlayerStateSnapshot state) =>
        new()
        {
            Id = state.Id,
            CurrentQueueItemId = string.IsNullOrEmpty(state.CurrentQueueItemId)
                ? null
                : state.CurrentQueueItemId,
            // В proto прогресс в целых секундах, а браузер ждёт строку hh:mm:ss.
            // Без разбора TimeSpan на часы клиент получал бы 0 и начинал трек
            // с нуля.
            CurrentTrackProgress = state.HasCurrentTrackProgress
                ? TimeSpan.FromSeconds(state.CurrentTrackProgressSeconds).ToString()
                : null,
            State = ToHubState(state.State),
            VideoState = ToHubState(state.VideoState),
            IsMuted = state.IsMuted,
            PausedByMute = state.PausedByMute,
            Volume = (float)state.Volume,
            CurrentQueueItem = state.CurrentQueueItem is null
                ? null
                : new QueueItemHubDto
                {
                    Id = state.CurrentQueueItem.Id,
                    Track = ToHubTrack(state.CurrentQueueItem.Track),
                },
        };

    /// <summary>Трек из proto в форму хаба.</summary>
    public static TrackInfoHubDto? ToHubTrack(TrackInfo? track) =>
        track is null
            ? null
            : new TrackInfoHubDto
            {
                Id = track.Id,
                TrackName = track.TrackName,
                Authors = [.. track.Authors],
                // В proto длительность в целых секундах, а браузер ждёт hh:mm:ss.
                Duration = TimeSpan.FromSeconds(track.DurationSeconds).ToString(),
                Url = track.Url,
                ArtworkUrl = track.ArtworkUrl,
                VideoId = track.VideoId,
            };

    /// <summary>Состояние воспроизведения.</summary>
    public static PlayerStateStateEnum ToHubState(ProtoPlaybackState state) =>
        state switch
        {
            ProtoPlaybackState.Playing => PlayerStateStateEnum.Playing,
            ProtoPlaybackState.Paused => PlayerStateStateEnum.Paused,
            _ => PlayerStateStateEnum.Stopped,
        };

    /// <summary>Режим видео.</summary>
    public static PlayerStateVideoStateEnum ToHubState(ProtoVideoDisplay state) =>
        state == ProtoVideoDisplay.Video
            ? PlayerStateVideoStateEnum.Video
            : PlayerStateVideoStateEnum.Hidden;
}
