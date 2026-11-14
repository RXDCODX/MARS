using System;
using System.Threading;
using System.Threading.Tasks;
using MARS.SoundRequest.Controllers;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Tests.Grpc;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.SoundRequest.Tests.Controllers;

/// <summary>
/// Внутренние REST-эндпоинты глушения плеера. Глушит координатор в MARS.Alerts,
/// поэтому проверяется ровно то, что видно в состоянии плеера.
/// </summary>
public class InternalSoundRequestControllerTests : IDisposable
{
    private readonly StateManager _stateManager;
    private readonly InternalSoundRequestController _controller;

    public InternalSoundRequestControllerTests()
    {
        _stateManager = new StateManager(
            new TestDbContextFactory(),
            new TestHostApplicationLifetime(),
            NullLogger<StateManager>.Instance
        );
        _controller = new InternalSoundRequestController(
            _stateManager,
            NullLogger<InternalSoundRequestController>.Instance
        );
    }

    public void Dispose()
    {
        _stateManager.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Играющий плеер не просто помечается заглушённым: воспроизведение
    /// приостанавливается, а флаг <c>PausedByMute</c> запоминает, что пауза —
    /// наша, чтобы снятие mute её и вернуло.
    /// </summary>
    [Fact]
    public async Task Глушение_Приостанавливает_Играющий_Плеер()
    {
        await _stateManager.SetPlaybackStateAsync(PlaybackState.Playing);

        await _controller.MuteAll(TestContext.Current.CancellationToken);

        var state = _stateManager.GetState();
        Assert.True(state.IsMuted);
        Assert.Equal(PlaybackState.Paused, state.State);
        Assert.True(state.PausedByMute);
    }

    [Fact]
    public async Task Снятие_Глушения_Возобновляет_Плеер()
    {
        await _stateManager.SetPlaybackStateAsync(PlaybackState.Playing);
        await _controller.MuteAll(TestContext.Current.CancellationToken);

        await _controller.UnmuteAll(TestContext.Current.CancellationToken);

        var state = _stateManager.GetState();
        Assert.False(state.IsMuted);
        Assert.Equal(PlaybackState.Playing, state.State);
        Assert.False(state.PausedByMute);
    }

    /// <summary>
    /// Пауза, поставленная не глушением, остаётся: координатор не должен
    /// запускать воспроизведение, которое владелец остановил сам.
    /// </summary>
    [Fact]
    public async Task Снятие_Глушения_Не_Запускает_Чужую_Паузу()
    {
        await _stateManager.SetPlaybackStateAsync(PlaybackState.Paused);

        await _controller.UnmuteAll(TestContext.Current.CancellationToken);

        var state = _stateManager.GetState();
        Assert.False(state.IsMuted);
        Assert.Equal(PlaybackState.Paused, state.State);
    }
}
