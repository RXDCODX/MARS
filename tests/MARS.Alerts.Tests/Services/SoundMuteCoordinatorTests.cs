using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Alerts.Services.Audio;
using MARS.Shared.Clients;
using MARS.Shared.Grpc.SoundRequest;
using MARS.Shared.Grpc.Voice;
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
    private readonly Mock<IAudioControllerClient> _audio = new();
    private readonly Mock<VoiceRecognitionService.VoiceRecognitionServiceClient> _tts = new(
        Mock.Of<CallInvoker>()
    );
    private readonly Mock<SoundRequestService.SoundRequestServiceClient> _soundRequest = new(
        Mock.Of<CallInvoker>()
    );

    public SoundMuteCoordinatorTests()
    {
        _tts.Setup(client =>
                client.SetMuteStateAsync(
                    It.IsAny<SetMuteStateRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Completed<SetMuteStateResponse>());

        _soundRequest
            .Setup(client =>
                client.MuteAllAsync(
                    It.IsAny<MuteAllRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Completed<MuteAllResponse>());

        _soundRequest
            .Setup(client =>
                client.UnmuteAllAsync(
                    It.IsAny<UnmuteAllRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Completed<UnmuteAllResponse>());
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
            client =>
                client.MuteAllAsync(
                    It.IsAny<MuteAllRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _tts.Verify(
            client =>
                client.SetMuteStateAsync(
                    It.Is<SetMuteStateRequest>(request => request.IsMuted && request.Volume == 0.0),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
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
            client =>
                client.UnmuteAllAsync(
                    It.IsAny<UnmuteAllRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _tts.Verify(
            client =>
                client.SetMuteStateAsync(
                    It.Is<SetMuteStateRequest>(request =>
                        !request.IsMuted && request.Volume == 1.0
                    ),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
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
                client.SetMuteStateAsync(
                    It.Is<SetMuteStateRequest>(request => request.IsMuted),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
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
            client =>
                client.MuteAllAsync(
                    It.IsAny<MuteAllRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _tts.Verify(
            client =>
                client.SetMuteStateAsync(
                    It.Is<SetMuteStateRequest>(request => request.IsMuted),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task TtsFailureDoesNotStopMixerLeg()
    {
        _tts.Setup(client =>
                client.SetMuteStateAsync(
                    It.IsAny<SetMuteStateRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "tts недоступен")));
        var coordinator = Create();

        await coordinator.MuteAsync();

        _audio.Verify(
            client =>
                client.MuteAsync(It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    private SoundMuteCoordinator Create() =>
        new(
            _audio.Object,
            _tts.Object,
            _soundRequest.Object,
            NullLogger<SoundMuteCoordinator>.Instance
        );

    private static AsyncUnaryCall<T> Completed<T>()
        where T : class, new() =>
        new(
            Task.FromResult(new T()),
            Task.FromResult(new Metadata()),
            () => new Status(),
            () => new Metadata(),
            () => { }
        );
}
