using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MARS.Alerts.Infrastructure.Audio;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.SoundRequest;
using MARS.Platform.Infrastructure.Grpc.Tts;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Models.Audio;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Координатор одновременного глушения звука: микшер (AudioController), TTS и
/// SoundRequest. Каждая нога — в своём try: недоступность одной не роняет остальные.
/// </summary>
public class SoundMuteCoordinatorTests
{
    private readonly Mock<IMarsGrpcClient> _grpc = new();
    private readonly Mock<IAudioControllerClient> _audio = new();
    private readonly Mock<ITtsApi> _tts = new();
    private readonly Mock<ISoundRequestApi> _soundRequest = new();

    public SoundMuteCoordinatorTests()
    {
        _grpc.SetupGet(client => client.Tts).Returns(_tts.Object);
        _grpc.SetupGet(client => client.SoundRequest).Returns(_soundRequest.Object);

        _audio
            .Setup(client =>
                client.MuteAsync(It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new MuteMixerResponse { MutedSessionCount = 1, MutedProcessNames = [] });
        _audio
            .Setup(client => client.UnmuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnmuteMixerResponse { UnmutedSessionCount = 1 });

        _tts.Setup(client =>
                client.SetMuteStateAsync(
                    It.IsAny<bool>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        _soundRequest
            .Setup(client => client.MuteAllAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _soundRequest
            .Setup(client => client.UnmuteAllAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task MuteContactsAllThreeLegs()
    {
        var coordinator = Create();

        await coordinator.MuteAsync();

        _audio.Verify(
            client =>
                client.MuteAsync(It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _soundRequest.Verify(
            client => client.MuteAllAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
        _tts.Verify(
            client => client.SetMuteStateAsync(true, 0.0, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UnmuteRestoresTtsVolume()
    {
        var coordinator = Create();

        await coordinator.MuteAsync();
        await coordinator.UnmuteAsync();

        _audio.Verify(client => client.UnmuteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _soundRequest.Verify(
            client => client.UnmuteAllAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
        _tts.Verify(
            client => client.SetMuteStateAsync(false, 1.0, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ExcludedProcessNamesAreForwardedToMixer()
    {
        string[]? excluded = ["obs64", "obs-browser-page"];
        var coordinator = Create();

        await coordinator.MuteAsync(excluded);

        _audio.Verify(
            client =>
                client.MuteAsync(
                    It.Is<IReadOnlyList<string>?>(names => names!.SequenceEqual(excluded)),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task TtsIsMutedOnlyOnce()
    {
        var coordinator = Create();

        await coordinator.MuteAsync();
        await coordinator.MuteAsync();

        _tts.Verify(
            client =>
                client.SetMuteStateAsync(true, It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _audio.Verify(
            client =>
                client.MuteAsync(It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task AudioControllerFailureDoesNotStopOtherLegs()
    {
        _audio
            .Setup(client =>
                client.MuteAsync(It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new AudioControllerUnavailableException("недоступен"));
        var coordinator = Create();

        await Assert.ThrowsAsync<AudioControllerUnavailableException>(() =>
            coordinator.MuteAsync()
        );

        _soundRequest.Verify(
            client => client.MuteAllAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
        _tts.Verify(
            client =>
                client.SetMuteStateAsync(true, It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task TtsFailureDoesNotStopMixerLeg()
    {
        _tts.Setup(client =>
                client.SetMuteStateAsync(
                    It.IsAny<bool>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new HttpRequestException("tts недоступен"));
        var coordinator = Create();

        await coordinator.MuteAsync();

        _audio.Verify(
            client =>
                client.MuteAsync(It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    private SoundMuteCoordinator Create() =>
        new(_audio.Object, _grpc.Object, NullLogger<SoundMuteCoordinator>.Instance);
}
