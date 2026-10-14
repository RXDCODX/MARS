using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Alerts.Hubs;
using MARS.Alerts.Services.Audio;
using MARS.Shared.Clients;
using MARS.Shared.Grpc.SoundRequest;
using MARS.Shared.Grpc.Voice;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using SoundRequestServiceClient = MARS.Shared.Grpc.SoundRequest.SoundRequestService.SoundRequestServiceClient;
using VoiceRecognitionServiceClient = MARS.Shared.Grpc.Voice.VoiceRecognitionService.VoiceRecognitionServiceClient;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Хаб оверлея — единственная точка входа для браузера: методы <c>MuteAll</c> и
/// <c>UnmuteSessions</c> проксируют в <see cref="SoundMuteCoordinator"/>. Здесь
/// проверяется именно проксирование, поведение координатора — в
/// <see cref="SoundMuteCoordinatorTests"/>.
/// </summary>
public class OverlayHubTests
{
    private readonly Mock<IAudioControllerClient> _audio = new();
    private readonly Mock<VoiceRecognitionServiceClient> _tts = new(Mock.Of<CallInvoker>());
    private readonly Mock<SoundRequestServiceClient> _soundRequest = new(Mock.Of<CallInvoker>());

    public OverlayHubTests()
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
    public async Task MuteAll_RelaysExcludedProcessesToCoordinator()
    {
        var hub = new OverlayHub(Create());

        await hub.MuteAll(new[] { "obs64", "obs-browser" });

        _audio.Verify(
            client =>
                client.MuteAsync(
                    It.Is<IReadOnlyList<string>>(names =>
                        names.SequenceEqual(new[] { "obs64", "obs-browser" })
                    ),
                    It.IsAny<CancellationToken>()
                ),
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
                    It.Is<SetMuteStateRequest>(request => request.IsMuted),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UnmuteSessions_RelaysToCoordinator()
    {
        var hub = new OverlayHub(Create());

        await hub.UnmuteSessions();

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
                    It.Is<SetMuteStateRequest>(request => !request.IsMuted),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
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
