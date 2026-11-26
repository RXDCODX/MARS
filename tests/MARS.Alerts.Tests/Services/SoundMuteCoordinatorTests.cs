using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MARS.Alerts.Infrastructure.Audio;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.AudioController;
using MARS.Platform.Infrastructure.Grpc.SoundRequest;
using MARS.Platform.Infrastructure.Grpc.Tts;
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
    private readonly Mock<IAudioControllerApi> _audio = new();
    private readonly Mock<ITtsApi> _tts = new();
    private readonly Mock<ISoundRequestApi> _soundRequest = new();

    public SoundMuteCoordinatorTests()
    {
        _grpc.SetupGet(client => client.AudioController).Returns(_audio.Object);
        _grpc.SetupGet(client => client.Tts).Returns(_tts.Object);
        _grpc.SetupGet(client => client.SoundRequest).Returns(_soundRequest.Object);

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

    /// <summary>
    /// Громкость TTS до глушения почётся единицей и возвращается по unmute —
    /// координатор не должен оставлять TTS заглушённым после снятия mute.
    /// </summary>
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

    /// <summary>
    /// Повторный mute не глушит TTS заново: переговор на уже заглушённую ногу
    /// ни к чему, а предыдущая громкость сохраняется от первого запроса.
    /// </summary>
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

    /// <summary>
    /// Сбой одной ноги не останавливает остальные и не выходит наружу исключением:
    /// оверлей всё равно должен получить «включено тихо», а не красную ошибку.
    /// </summary>
    [Fact]
    public async Task AudioControllerFailureDoesNotStopOtherLegs()
    {
        _audio
            .Setup(client =>
                client.MuteAsync(It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new HttpRequestException("недоступен"));
        var coordinator = Create();

        await coordinator.MuteAsync();

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
        new(_grpc.Object, NullLogger<SoundMuteCoordinator>.Instance);
}
