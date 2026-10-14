using MARS.AudioController.Configuration;
using MARS.AudioController.Controllers;
using MARS.AudioController.Services;
using MARS.Shared.Models;
using MARS.Shared.Models.Audio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.AudioController.Tests.Controllers;

public class MixerControllerTests
{
    private readonly Mock<IAudioMixerService> _audioMixerServiceMock = new();
    private readonly AudioControllerConfiguration _configuration = new();

    private MixerController CreateController()
    {
        var options = Options.Create(_configuration);

        return new MixerController(
            _audioMixerServiceMock.Object,
            options,
            NullLogger<MixerController>.Instance
        );
    }

    [Fact]
    public async Task Mute_WithExcludedProcessNames_CallsService()
    {
        var excluded = new List<string> { "obs64" };

        _audioMixerServiceMock
            .Setup(service => service.MuteAsync(excluded, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "chrome", "spotify" });

        var controller = CreateController();
        var result = await controller.Mute(
            new MuteMixerRequest { ExcludedProcessNames = excluded },
            TestContext.Current.CancellationToken
        );

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MuteMixerResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Equal(2, envelope.Result!.MutedSessionCount);
        Assert.Contains("chrome", envelope.Result.MutedProcessNames);
        Assert.Contains("spotify", envelope.Result.MutedProcessNames);
    }

    [Fact]
    public async Task Mute_WithoutBody_UsesDefaultExclusions()
    {
        var defaultExclusions = new List<string>(_configuration.DefaultExcludedProcessNames);

        _audioMixerServiceMock
            .Setup(service => service.MuteAsync(defaultExclusions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "discord" });

        var controller = CreateController();
        var result = await controller.Mute(null, TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MuteMixerResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Equal(1, envelope.Result!.MutedSessionCount);
    }

    [Fact]
    public async Task Mute_ServiceThrows_ReturnsFail()
    {
        _audioMixerServiceMock
            .Setup(service =>
                service.MuteAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Audio session manager unavailable"));

        var controller = CreateController();
        var result = await controller.Mute(null, TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MuteMixerResponse>>(okResult.Value);

        Assert.False(envelope.Success);
        Assert.Contains("Audio session manager unavailable", envelope.ErrorMessage);
    }

    [Fact]
    public async Task Unmute_CallsService()
    {
        _audioMixerServiceMock
            .Setup(service => service.UnmuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var controller = CreateController();
        var result = await controller.Unmute(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<UnmuteMixerResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Equal(3, envelope.Result!.UnmutedSessionCount);
    }

    [Fact]
    public async Task Unmute_ServiceThrows_ReturnsFail()
    {
        _audioMixerServiceMock
            .Setup(service => service.UnmuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No sessions"));

        var controller = CreateController();
        var result = await controller.Unmute(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<UnmuteMixerResponse>>(okResult.Value);

        Assert.False(envelope.Success);
    }

    [Fact]
    public async Task GetMutedSessions_ReturnsSessions()
    {
        var sessions = new List<MutedSessionInfo>
        {
            new(1234, "chrome", "Chrome"),
            new(5678, "spotify", "Spotify"),
        };

        _audioMixerServiceMock
            .Setup(service => service.GetMutedSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var controller = CreateController();
        var result = await controller.GetMutedSessions(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MutedSessionsResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Equal(2, envelope.Result!.Sessions.Count);
        Assert.Equal("chrome", envelope.Result.Sessions[0].ProcessName);
        Assert.Equal(1234u, envelope.Result.Sessions[0].ProcessId);
    }

    [Fact]
    public async Task GetMutedSessions_Empty_ReturnsEmptyList()
    {
        _audioMixerServiceMock
            .Setup(service => service.GetMutedSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MutedSessionInfo>());

        var controller = CreateController();
        var result = await controller.GetMutedSessions(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MutedSessionsResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Empty(envelope.Result!.Sessions);
    }
}
