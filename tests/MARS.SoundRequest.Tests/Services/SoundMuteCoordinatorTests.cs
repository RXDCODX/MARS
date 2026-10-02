using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.SoundBarService;
using MARS.SoundRequest.Tests.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Согласование «заглушить звук» с состоянием плеера.
///
/// Ключевое правило: если музыка играла, заглушение ставит её на паузу, а снятие
/// заглушения снимает паузу. Без этого после команды «выключить звук» плеер
/// замолкал навсегда: состояние плеера и реальный звук разъезжались.
/// </summary>
public class SoundMuteCoordinatorTests
{
    private readonly TestDbContextFactory _factory = new(
        new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"sound-{Guid.NewGuid():N}")
            .Options
    );
    private readonly TestHostApplicationLifetime _lifetime = new();

    [Fact]
    public async Task MuteMarksSoundBarAsMuted()
    {
        var coordinator = Create(out var state);

        await coordinator.MuteAsync("obs64");

        Assert.True((await state.GetStateAsync()).IsMuted);
    }

    /// <summary>
    /// Заглушение играющей музыки ставит её на паузу и запоминает, что пауза
    /// сделана именно заглушением: снятие заглушения должно вернуть игру.
    /// </summary>
    [Fact]
    public async Task MutePausesPlayingTrack()
    {
        var coordinator = Create(out var state);
        await state.SetPlaybackStateAsync(PlaybackState.Playing);

        await coordinator.MuteAsync("obs64");

        var snapshot = await state.GetStateAsync();
        Assert.Equal(PlaybackState.Paused, snapshot.State);
        Assert.True(snapshot.PausedByMute);
    }

    /// <summary>
    /// Остановленный плеер заглушением не трогается: иначе после снятия
    /// заглушения он «ожил» бы сам.
    /// </summary>
    [Fact]
    public async Task MuteDoesNotPauseStoppedTrack()
    {
        var coordinator = Create(out var state);

        await coordinator.MuteAsync("obs64");

        Assert.False((await state.GetStateAsync()).PausedByMute);
    }

    [Fact]
    public async Task UnmuteResumesTrackPausedByMute()
    {
        var coordinator = Create(out var state);
        await state.SetPlaybackStateAsync(PlaybackState.Playing);
        await coordinator.MuteAsync("obs64");

        await coordinator.UnmuteAsync();

        var snapshot = await state.GetStateAsync();
        Assert.False(snapshot.IsMuted);
        Assert.False(snapshot.PausedByMute);
        Assert.Equal(PlaybackState.Playing, snapshot.State);
    }

    /// <summary>
    /// Ошибка контроллера звука не должна оставлять состояние несогласованным:
    /// флаг «заглушено» ставится в любом случае, иначе повторная попытка
    /// заглушить перезапустила бы загрузку.
    /// </summary>
    [Fact]
    public async Task FailedMuteStillMarksMuted()
    {
        var state = NewStateManager();
        var coordinator = new SoundMuteCoordinator(
            () => throw new InvalidOperationException("контроллер недоступен"),
            state,
            NullLogger<SoundMuteCoordinator>.Instance
        );

        await coordinator.MuteAsync("obs64");

        Assert.True((await state.GetStateAsync()).IsMuted);
    }

    [Fact]
    public async Task FailedUnmuteClearsFlag()
    {
        var state = NewStateManager();
        var coordinator = new SoundMuteCoordinator(
            () => throw new InvalidOperationException("контроллер недоступен"),
            state,
            NullLogger<SoundMuteCoordinator>.Instance
        );

        await coordinator.UnmuteAsync();

        Assert.False((await state.GetStateAsync()).IsMuted);
    }

    [Fact]
    public async Task MuteWithoutProcessesIsSafe()
    {
        var coordinator = Create(out _);

        await coordinator.MuteAsync();
    }

    private SoundMuteCoordinator Create(out StateManager state)
    {
        state = NewStateManager();

        return new SoundMuteCoordinator(
            () => new SoundBarServiceLocal(),
            state,
            NullLogger<SoundMuteCoordinator>.Instance
        );
    }

    private StateManager NewStateManager() =>
        new(_factory, _lifetime, NullLogger<StateManager>.Instance);
}
