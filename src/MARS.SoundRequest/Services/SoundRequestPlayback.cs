using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Hubs.Dtos;
using MARS.SoundRequest.Services.Interfaces;

namespace MARS.SoundRequest.Services;

/// <inheritdoc cref="ISoundRequestPlayback" />
public sealed class SoundRequestPlayback(
    StateManager stateManager,
    IPlayerController playerController,
    TrackEventRelay trackEventRelay
) : ISoundRequestPlayback
{
    public async Task StartedAsync(TrackInfoHubDto track, CancellationToken cancellationToken)
    {
        await trackEventRelay.OnStartedInvoke(ToDomain(track));
    }

    public async Task EndedAsync(TrackInfoHubDto track, CancellationToken cancellationToken)
    {
        await trackEventRelay.OnEndedInvoke(ToDomain(track));
    }

    public async Task ErrorPlayingAsync(TrackInfoHubDto track, CancellationToken cancellationToken)
    {
        await trackEventRelay.OnErrorInvoke(ToDomain(track));
    }

    /// <summary>
    /// Состояние из браузера переносится в состояние сервиса.
    /// </summary>
    /// <remarks>
    /// Отдельный метод существует из-за прогресса: клиент присылает строку
    /// <c>hh:mm:ss</c>, а доменное состояние хранит <see cref="TimeSpan" />.
    /// Без разбора строки плеер получал бы прогресс в 3600 раз меньше и
    /// продолжал бы с нуля.
    /// </remarks>
    public async Task FrontStateChangeAsync(
        PlayerStateHubDto state,
        CancellationToken cancellationToken
    )
    {
        if (state.State == PlayerStateStateEnum.Playing)
        {
            await playerController.EnsureCurrentQueueItemLoadedAsync();
        }

        await stateManager.UpdateStateAsync(current =>
        {
            current.State = ToDomain(state.State);
            current.IsMuted = state.IsMuted;
            current.Volume = state.Volume;
            current.VideoState = ToDomain(state.VideoState);
            current.CurrentTrackProgress = ParseProgress(state.CurrentTrackProgress);
        });
    }

    public async Task TrackProgressAsync(
        double seconds,
        string? excludeSubscriberId,
        CancellationToken cancellationToken
    ) =>
        await stateManager.UpdateCurrentTrackProgressAsync(
            TimeSpan.FromSeconds(seconds),
            notify: false,
            excludeSubscriberId
        );

    public async Task SkipAsync(CancellationToken cancellationToken) =>
        await playerController.SkipAsync(cancellationToken);

    public async Task PlayPreviousAsync(CancellationToken cancellationToken) =>
        await playerController.PlayPreviousFromHistoryAsync();

    private static PlaybackState ToDomain(PlayerStateStateEnum state) =>
        state switch
        {
            PlayerStateStateEnum.Playing => PlaybackState.Playing,
            PlayerStateStateEnum.Paused => PlaybackState.Paused,
            _ => PlaybackState.Stopped,
        };

    private static VideoDisplay ToDomain(PlayerStateVideoStateEnum state) =>
        state == PlayerStateVideoStateEnum.Video ? VideoDisplay.Video : VideoDisplay.NoVideo;

    /// <summary>
    /// Разбирает прогресс из строки <c>hh:mm:ss</c>.
    /// </summary>
    /// <remarks>
    /// Неразбираемая строка даёт <c>null</c>, а не исключение: сюда приходит
    /// то, что набрал браузер, и обрыв из-за чужой строки уронил бы плеер
    /// целиком. Отсутствие прогресса и неверная запись означают одно и то же —
    /// начинать с нуля.
    /// </remarks>
    private static TimeSpan? ParseProgress(string? value) =>
        TimeSpan.TryParse(value, out var parsed) ? parsed : null;

    private static BaseTrackInfo ToDomain(TrackInfoHubDto track) =>
        new()
        {
            Id = Guid.TryParse(track.Id, out var id) ? id : Guid.Empty,
            TrackName = track.TrackName,
            Authors = track.Authors,
            Duration = ParseDuration(track.Duration),
            // Ссылка у клиента приходит строкой; без неё объект не создаётся
            // вовсе, а трек без адреса всё равно пригоден для события старта и
            // окончания — там важен идентификатор.
            Url = Uri.TryCreate(track.Url, UriKind.Absolute, out var url)
                ? url
                : new Uri("about:blank"),
            ArtworkUrl = Uri.TryCreate(track.ArtworkUrl, UriKind.Absolute, out var artwork)
                ? artwork
                : null,
            VideoId = track.VideoId,
        };

    /// <summary>
    /// Разбирает длительность из строки <c>hh:mm:ss</c>.
    /// </summary>
    /// <remarks>
    /// Неразбираемая строка даёт <see cref="TimeSpan.Zero" />, а не исключение:
    /// иначе один кривой символ в прогрессе трека уронил бы событие окончания
    /// вместе с подписчиками.
    /// </remarks>
    private static TimeSpan ParseDuration(string? value) =>
        TimeSpan.TryParse(value, out var parsed) ? parsed : TimeSpan.Zero;
}
