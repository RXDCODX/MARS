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

public class MicrophoneControllerTests
{
    private readonly Mock<IAudioDeviceService> _audioDeviceServiceMock = new();
    private readonly AudioControllerConfiguration _configuration = new();

    private MicrophoneController CreateController()
    {
        var options = Options.Create(_configuration);

        return new MicrophoneController(
            _audioDeviceServiceMock.Object,
            NullLogger<MicrophoneController>.Instance
        );
    }

    [Fact]
    public async Task GetVolume_ReturnsVolume()
    {
        _audioDeviceServiceMock
            .Setup(service => service.GetVolumeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.8f);
        _audioDeviceServiceMock
            .Setup(service => service.GetDeviceNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Microphone");
        _audioDeviceServiceMock
            .Setup(service => service.IsMutedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController();
        var result = await controller.GetVolume(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MicrophoneVolumeResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Equal(0.8f, envelope.Result!.Volume);
        Assert.Equal("80%", envelope.Result.VolumePercent);
        Assert.False(envelope.Result.IsMuted);
        Assert.False(envelope.Result.IsAtMaximum);
        Assert.Equal("Microphone", envelope.Result.DeviceName);
    }

    [Fact]
    public async Task GetVolume_AtMaximum_ReturnsTrue()
    {
        _audioDeviceServiceMock
            .Setup(service => service.GetVolumeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.0f);
        _audioDeviceServiceMock
            .Setup(service => service.GetDeviceNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Microphone");
        _audioDeviceServiceMock
            .Setup(service => service.IsMutedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController();
        var result = await controller.GetVolume(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MicrophoneVolumeResponse>>(okResult.Value);

        Assert.True(envelope.Result!.IsAtMaximum);
    }

    [Fact]
    public async Task SetVolume_ValidVolume_ReturnsOk()
    {
        _audioDeviceServiceMock
            .Setup(service => service.GetVolumeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.5f);
        _audioDeviceServiceMock
            .Setup(service => service.SetVolumeAsync(0.7f, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        var result = await controller.SetVolume(0.7f, TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MicrophoneVolumeSetResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Equal(0.5f, envelope.Result!.PreviousVolume);
        Assert.Equal(0.7f, envelope.Result.CurrentVolume);
        Assert.Equal("70%", envelope.Result.CurrentVolumePercent);
    }

    [Fact]
    public async Task SetVolume_OutOfRange_ReturnsFail()
    {
        var controller = CreateController();
        var result = await controller.SetVolume(1.5f, TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MicrophoneVolumeSetResponse>>(okResult.Value);

        Assert.False(envelope.Success);
        Assert.Contains("0.0 and 1.0", envelope.ErrorMessage);
    }

    [Fact]
    public async Task SetVolume_Negative_ReturnsFail()
    {
        var controller = CreateController();
        var result = await controller.SetVolume(-0.1f, TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MicrophoneVolumeSetResponse>>(okResult.Value);

        Assert.False(envelope.Success);
    }

    [Fact]
    public async Task SetVolumeToMax_CallsService()
    {
        _audioDeviceServiceMock
            .Setup(service => service.GetVolumeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.3f);
        _audioDeviceServiceMock
            .Setup(service => service.SetVolumeToMaxAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        var result = await controller.SetVolumeToMax(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MicrophoneVolumeSetResponse>>(okResult.Value);

        Assert.True(envelope.Success);
        Assert.Equal(1.0f, envelope.Result!.CurrentVolume);
        Assert.Equal("100%", envelope.Result.CurrentVolumePercent);
        Assert.Equal("Microphone volume set to maximum", envelope.Result.Message);
    }

    [Fact]
    public async Task GetVolume_ServiceThrows_ReturnsFail()
    {
        _audioDeviceServiceMock
            .Setup(service => service.GetVolumeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No device"));

        var controller = CreateController();
        var result = await controller.GetVolume(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<OperationResult<MicrophoneVolumeResponse>>(okResult.Value);

        Assert.False(envelope.Success);
        Assert.Contains("No device", envelope.ErrorMessage);
    }
}
