using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Models.Control;
using MARS.Shared.Kernel;
using MARS.TTS.Controllers;
using MARS.TTS.Models;
using MARS.TTS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.TTS.Tests.Controllers;

/// <summary>
/// Внутренний REST-эндпоинт глушения TTS: координатор в MARS.Alerts глушит
/// микшер, плеер и синтез одновременно, и синтез для него — одна из ног.
/// </summary>
public class TtsInternalControllerTests
{
    private readonly Mock<ITtsNotifier> _notifier = new();

    [Fact]
    public async Task Состояние_Глушения_Рассылается_Подписчикам()
    {
        var controller = Create();

        var result = await controller.SetMuteState(
            new TtsMuteStateRequest { IsMuted = true, Volume = 0 },
            TestContext.Current.CancellationToken
        );

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<OperationResult>(ok.Value).Success);

        _notifier.Verify(
            notifier =>
                notifier.BroadcastStateAsync(
                    It.Is<TtsState>(state => state.IsStopped && state.Volume == 0),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Снятие_Глушения_Возвращает_Синтез_И_Громкость()
    {
        var controller = Create();

        await controller.SetMuteState(
            new TtsMuteStateRequest { IsMuted = false, Volume = 100 },
            TestContext.Current.CancellationToken
        );

        _notifier.Verify(
            notifier =>
                notifier.BroadcastStateAsync(
                    It.Is<TtsState>(state => !state.IsStopped && state.Volume == 100),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Пустое_Состояние_Отклоняется_Статусом_400()
    {
        var controller = Create();

        var result = await controller.SetMuteState(null!, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _notifier.Verify(
            notifier =>
                notifier.BroadcastStateAsync(It.IsAny<TtsState?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private TtsInternalController Create() =>
        new(_notifier.Object, NullLogger<TtsInternalController>.Instance);
}
