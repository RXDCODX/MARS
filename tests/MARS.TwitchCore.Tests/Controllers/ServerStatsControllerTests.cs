using MARS.Shared.Models;
using MARS.TwitchCore.Controllers;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.Connection;
using MARS.TwitchCore.Services.EventSub;
using MARS.TwitchCore.Services.PuntoSwitcher;
using MARS.TwitchCore.Services.WeddingAnniversary;
using MARS.TwitchCore.Tests.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Controllers;

/// <summary>
/// Статистика сервера.
///
/// Эндпоинт показывает оператору состояние стенда: память, время работы, подключение
/// чата Twitch и ближайшую годовщину. Если он перестанет отвечать или начнёт врать о
/// подключении, у оператора не будет чем проверять работу сервисов.
/// </summary>
public class ServerStatsControllerTests
{
    private readonly Mock<ITwitchConnectionState> _connection = new();
    private readonly Mock<IPuntoSwitcherService> _punto = new();
    private readonly Mock<WeddingAnniversaryService> _anniversary = new();
    private readonly Mock<EventSubService> _eventSub = new();
    private readonly ServerStatsController _controller;

    public ServerStatsControllerTests()
    {
        // Менеджер подключения и служба EventSub создают клиентов TwitchLib, поэтому
        // подменяются только те их члены, которые читает контроллер.
        _eventSub = new Mock<EventSubService>(
            Mock.Of<ITwitchAPI>(),
            NullLogger<EventSubService>.Instance,
            new TokenService(
                Mock.Of<ITwitchAPI>(),
                NullLogger<TokenService>.Instance,
                new TwitchTestDbContextFactory()
            ),
            Mock.Of<Microsoft.Extensions.Hosting.IHostApplicationLifetime>(),
            OfflineEventSub.Create()
        );
        _anniversary = new Mock<WeddingAnniversaryService>(
            new TwitchTestDbContextFactory(),
            NullLogger<WeddingAnniversaryService>.Instance
        );
        _anniversary
            .Setup(service => service.GetNearestAnniversaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((NearestAnniversaryDto?)null);
        _eventSub.SetupGet(service => service.IsWebSocketConnected).Returns(true);
        _connection.SetupGet(service => service.IsConnected).Returns(true);
        _punto.SetupGet(service => service.IsFilterEnabled).Returns(false);

        _controller = new ServerStatsController(
            NullLogger<ServerStatsController>.Instance,
            _eventSub.Object,
            _connection.Object,
            _anniversary.Object,
            _punto.Object
        );
    }

    /// <summary>
    /// Статистика отдаётся как успешный результат с реальными числами процесса:
    /// оператор видит фактическое потребление памяти и время работы.
    /// </summary>
    [Fact]
    public async Task StatsDescribeRunningProcess()
    {
        var response = await _controller.GetStats(TestContext.Current.CancellationToken);

        var result = Assert.IsType<OkObjectResult>(response.Result).Value;
        var payload = Assert.IsType<OperationResult<ServerStatsResponse>>(result);

        Assert.True(payload.Success);
        Assert.NotNull(payload.Result);
        Assert.True(payload.Result.ThreadCount > 0);
        Assert.True(payload.Result.MemoryWorkingSetBytes > 0);
        Assert.True(payload.Result.UptimeSeconds >= 0);
    }

    /// <summary>
    /// Состояние чата и фильтра попадает в ответ: по нему оператор видит, что
    /// подключение Twitch живо, а фильтр выключен.
    /// </summary>
    [Fact]
    public async Task StatsReportConnectionAndFilterState()
    {
        var response = await _controller.GetStats(TestContext.Current.CancellationToken);

        var payload = Assert.IsType<OperationResult<ServerStatsResponse>>(
            Assert.IsType<OkObjectResult>(response.Result).Value
        );

        Assert.True(payload.Result!.IsEventSubConnected);
        Assert.True(payload.Result.IsTwitchChatConnected);
        Assert.False(payload.Result.IsPuntoSwitcherEnabled);
    }

    /// <summary>
    /// Ближайшая годовщина подставляется в ответ: без неё оператор не увидит
    /// напоминание, ради которого и открывает страницу.
    /// </summary>
    [Fact]
    public async Task StatsIncludeNearestAnniversary()
    {
        var anniversary = new NearestAnniversaryDto
        {
            TwitchId = "123456789",
            AnniversaryName = "день рождения канала",
            AnniversaryDate = new DateTime(2026, 6, 1),
            DisplayName = "зритель",
        };
        _anniversary
            .Setup(service => service.GetNearestAnniversaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(anniversary);

        var response = await _controller.GetStats(TestContext.Current.CancellationToken);

        var payload = Assert.IsType<OperationResult<ServerStatsResponse>>(
            Assert.IsType<OkObjectResult>(response.Result).Value
        );

        Assert.Equal("день рождения канала", payload.Result!.NearestWeddingAnniversaryName);
        Assert.Equal("зритель", payload.Result.NearestWeddingAnniversaryUser);
    }

    /// <summary>
    /// Ошибка чтения статистики не роняет эндпоинт: оператор видит сообщение об
    /// ошибке, а не 500.
    /// </summary>
    [Fact]
    public async Task FailureIsReportedAsResult()
    {
        _anniversary
            .Setup(service => service.GetNearestAnniversaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("база недоступна"));

        var response = await _controller.GetStats(TestContext.Current.CancellationToken);

        var payload = Assert.IsType<OperationResult<ServerStatsResponse>>(
            Assert.IsType<OkObjectResult>(response.Result).Value
        );

        Assert.False(payload.Success);
        Assert.Null(payload.Result);
    }
}
