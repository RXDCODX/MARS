using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services.Interfaces;

namespace MARS.SoundRequest.Tests.Grpc;

/// <summary>
/// Заглушка плеера: MainPlayer тянет Spotify, SoundBar и очередь, а проверять
/// нужно только то, что gRPC-вызовы до него доходят.
/// </summary>
public sealed class FakePlayerController : IPlayerController
{
    public int EnsureCurrentQueueItemLoadedCalls { get; private set; }

    public int SkipCalls { get; private set; }

    public int PlayPreviousCalls { get; private set; }

    public VideoDisplay? LastVideoDisplay { get; private set; }

    public PlayerState State { get; set; } = new() { Id = Guid.NewGuid(), Volume = 100f };

    public Task PauseAsync(CancellationToken ct) => Task.CompletedTask;

    public Task ResumeAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public Task SkipAsync(CancellationToken ct)
    {
        SkipCalls++;
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(float volume, CancellationToken ct) => Task.CompletedTask;

    public Task MuteAsync(CancellationToken ct) => Task.CompletedTask;

    public Task UnmuteAsync(CancellationToken ct) => Task.CompletedTask;

    public Task SetVideoDisplayAsync(VideoDisplay videoDisplay, CancellationToken ct)
    {
        LastVideoDisplay = videoDisplay;
        return Task.CompletedTask;
    }

    public Task EnsureCurrentQueueItemLoadedAsync()
    {
        EnsureCurrentQueueItemLoadedCalls++;
        return Task.CompletedTask;
    }

    public Task PlayPreviousFromHistoryAsync()
    {
        PlayPreviousCalls++;
        return Task.CompletedTask;
    }

    public PlayerState GetState() => State;
}
