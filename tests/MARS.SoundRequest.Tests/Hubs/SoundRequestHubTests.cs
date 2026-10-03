using MARS.SoundRequest.Hubs;
using MARS.SoundRequest.Hubs.Dtos;
using MARS.SoundRequest.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.SoundRequest.Tests.Hubs;

/// <summary>
/// Клиентские команды хаба очереди звуковых запросов.
/// </summary>
/// <remarks>
/// <para>
/// Раньше <c>SoundRequestHub</c> был классом без единого метода: он только
/// рассылал события. Клиент же шлёт в него <c>Started</c>, <c>Ended</c>,
/// <c>ErrorPlaying</c>, <c>FrontStateChange</c>, <c>TrackProgress</c>,
/// <c>SkipTrack</c> и <c>PlayPrevious</c> — все семь реализованы были только в
/// gRPC-сервисе, а браузер до gRPC не ходит.
/// </para>
/// <para>
/// Симптом на стенде: play, пауза, стоп, перемотка, предыдущий трек, громкость и
/// режим видео не делают ничего, в консоли на каждое нажатие «Method does not
/// exist», а экраны выглядят живыми, потому что состояние берётся по REST.
/// </para>
/// <para>
/// Тест проверяет делегирование, а не работу исполнителей: у каждого есть свои
/// тесты, а здесь важно, что команда клиента доходит до нужного сервиса и что
/// опечатка в имени метода ломает сборку, а не поведение на стенде.
/// </para>
/// </remarks>
public class SoundRequestHubTests
{
    private readonly Mock<ISoundRequestPlayback> playback = new();

    private SoundRequestHub CreateHub() => new(playback.Object);

    [Fact]
    public async Task Skip_track_goes_to_playback()
    {
        var hub = CreateHub();

        await hub.SkipTrack(TestContext.Current.CancellationToken);

        playback.Verify(service => service.SkipAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Play_previous_goes_to_playback()
    {
        var hub = CreateHub();

        await hub.PlayPrevious(TestContext.Current.CancellationToken);

        playback.Verify(
            service => service.PlayPreviousAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Track_started_goes_to_relay()
    {
        var hub = CreateHub();

        await hub.Started(
            new TrackInfoHubDto { Id = "a", TrackName = "Kasane Teto" },
            TestContext.Current.CancellationToken
        );

        playback.Verify(
            service =>
                service.StartedAsync(
                    It.Is<TrackInfoHubDto>(track => track.TrackName == "Kasane Teto"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Track_ended_goes_to_relay()
    {
        var hub = CreateHub();

        await hub.Ended(new TrackInfoHubDto { Id = "a" }, TestContext.Current.CancellationToken);

        playback.Verify(
            service =>
                service.EndedAsync(It.IsAny<TrackInfoHubDto>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Playback_error_goes_to_relay()
    {
        var hub = CreateHub();

        await hub.ErrorPlaying(
            new TrackInfoHubDto { Id = "a" },
            TestContext.Current.CancellationToken
        );

        playback.Verify(
            service =>
                service.ErrorPlayingAsync(
                    It.IsAny<TrackInfoHubDto>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Front_state_change_reaches_playback_with_mapped_state()
    {
        var hub = CreateHub();

        await hub.FrontStateChange(
            new PlayerStateHubDto
            {
                State = PlayerStateStateEnum.Playing,
                VideoState = PlayerStateVideoStateEnum.Video,
                IsMuted = true,
                Volume = 42,
                CurrentTrackProgress = "00:01:30",
            },
            TestContext.Current.CancellationToken
        );

        playback.Verify(
            service =>
                service.FrontStateChangeAsync(
                    It.Is<PlayerStateHubDto>(state =>
                        state.State == PlayerStateStateEnum.Playing
                        && state.IsMuted
                        && state.Volume == 42
                        // «00:01:30» обязан доехать как полторы минуты, иначе
                        // плеер получал бы прогресс в сто раз меньше.
                        && state.CurrentTrackProgress == "00:01:30"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Track_progress_reaches_playback_in_seconds()
    {
        var hub = CreateHub();

        await hub.TrackProgress(90.5, TestContext.Current.CancellationToken);

        playback.Verify(
            service => service.TrackProgressAsync(90.5, null, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
