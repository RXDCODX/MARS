using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MARS.Alerts.Infrastructure.Integrations.Audio;
using MARS.Platform.Infrastructure.Clients.Http;
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
    private readonly Mock<ITtsControlClient> _tts = new();
    private readonly Mock<ISoundRequestControlClient> _soundRequest = new();

    public SoundMuteCoordinatorTests()
    {
        _tts.Setup(client =>
                client.SetMuteStateAsync(
                    It.IsAny<bool>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _soundRequest.Setup(client => client.MuteAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _soundRequest.Setup(client => client.UnmuteAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
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
            client =>
                client.SetMuteStateAsync(
                    true,
                    0.0,
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
            client => client.UnmuteAllAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
        _tts.Verify(
            client =>
                client.SetMuteStateAsync(
                    false,
                    1.0,
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
                    true,
                    It.IsAny<double>(),
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
    /// Сервис ответил, что не заглушил: флаг успеха не выставляется, поэтому
    /// следующий mute снова просит TTS замолчать, а не считает работу сделанной.
    /// </summary>
    [Fact]
    public async Task TtsRefusalKeepsMuteNotApplied()
    {
        _tts.Setup(client =>
                client.SetMuteStateAsync(
                    It.IsAny<bool>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(false);
        var coordinator = Create();

        await coordinator.MuteAsync();
        await coordinator.MuteAsync();

        _tts.Verify(
            client =>
                client.SetMuteStateAsync(
                    true,
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()
                ),
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
                client.SetMuteStateAsync(
                    true,
                    It.IsAny<double>(),
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
        new(
            _audio.Object,
            _tts.Object,
            _soundRequest.Object,
            NullLogger<SoundMuteCoordinator>.Instance
        );
}
