using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Alerts.Infrastructure.Integrations.Audio;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Modules.Alerts.Application.Adhd;
using MARS.Modules.Alerts.Application.Adhd.Models;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Telegramus;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Хаб оверлея — единственная точка входа для браузера: методы <c>MuteAll</c> и
/// <c>UnmuteSessions</c> проксируют в <see cref="SoundMuteCoordinator"/>, а
/// <c>UpdateAdhdConfig</c>/<c>OnConnectedAsync</c> читают и пишут раскладку
/// ADHD-экрана через <see cref="IAdhdLayoutService"/>.
/// </summary>
/// <remarks>
/// Рассылка идёт через <see cref="IHubContext{THub,T}"/>, поэтому получатели
/// видны здесь: <c>Clients</c> самого хаба в юнит-тесте недоступен.
/// </remarks>
public class OverlayHubTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Mock<IAudioControllerClient> _audio = new();
    private readonly Mock<ITtsControlClient> _tts = new();
    private readonly Mock<ISoundRequestControlClient> _soundRequest = new();
    private readonly Mock<IAdhdLayoutService> _adhdLayout = new();
    private readonly Mock<IOverlayHub> _clients = new();
    private readonly Mock<IHubClients<IOverlayHub>> _hubClients = new();

    public OverlayHubTests()
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

        _hubClients
            .Setup(clients => clients.AllExcept(It.IsAny<IReadOnlyList<string>>()))
            .Returns(_clients.Object);
        _hubClients.Setup(clients => clients.Client(It.IsAny<string>())).Returns(_clients.Object);
        _clients
            .Setup(client => client.AdhdConfig(It.IsAny<AdhdConfigEvent>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task MuteAll_RelaysExcludedProcessesToCoordinator()
    {
        var hub = CreateHub();

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
    public async Task UnmuteSessions_RelaysToCoordinator()
    {
        var hub = CreateHub();

        await hub.UnmuteSessions();

        _audio.Verify(client => client.UnmuteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _soundRequest.Verify(
            client => client.UnmuteAllAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
        _tts.Verify(
            client =>
                client.SetMuteStateAsync(
                    false,
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Сохранённая раскладка уходит остальным оверлеям, а не только автору:
    /// открытая в другой вкладке панель должна перерисоваться без перезагрузки.
    /// </summary>
    [Fact]
    public async Task UpdateAdhdConfig_WritesAndNotifiesOtherOverlays()
    {
        var config = new AdhdLayoutConfigDto { ShowRainEffect = false, DvdLogosCount = 5 };
        AdhdConfigEvent? sent = null;

        _adhdLayout
            .Setup(service => service.UpdateAsync(config, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<AdhdLayoutConfigDto>.Ok(config));
        _clients
            .Setup(client => client.AdhdConfig(It.IsAny<AdhdConfigEvent>()))
            .Callback<AdhdConfigEvent>(payload => sent = payload)
            .Returns(Task.CompletedTask);

        var saved = await CreateHub().UpdateAdhdConfig(config);

        Assert.True(saved);
        Assert.NotNull(sent);
        _hubClients.Verify(
            clients =>
                clients.AllExcept(
                    It.Is<IReadOnlyList<string>>(ids => ids.Contains("connection-1"))
                ),
            Times.Once
        );

        var received = JsonSerializer.Deserialize<AdhdLayoutConfigDto>(
            sent!.ConfigJson.ToStringUtf8(),
            JsonOptions
        );

        Assert.NotNull(received);
        Assert.False(received!.ShowRainEffect);
        Assert.Equal(5, received.DvdLogosCount);
    }

    /// <summary>
    /// Отказ записи — это <see langword="false"/>, а не исключение: панель уже
    /// перерисована из локального состояния и должна узнать, что сохранять было
    /// некуда, но не сломаться от этого.
    /// </summary>
    [Fact]
    public async Task UpdateAdhdConfig_ReturnsFalseWhenWriteFails()
    {
        _adhdLayout
            .Setup(service =>
                service.UpdateAsync(It.IsAny<AdhdLayoutConfigDto>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                OperationResult<AdhdLayoutConfigDto>.Fail(
                    "база недоступна",
                    HttpStatusCode.ServiceUnavailable
                )
            );

        var saved = await CreateHub().UpdateAdhdConfig(new AdhdLayoutConfigDto());

        Assert.False(saved);
        _clients.Verify(client => client.AdhdConfig(It.IsAny<AdhdConfigEvent>()), Times.Never);
    }

    /// <summary>
    /// Подключившаяся вкладка сразу получает сохранённую раскладку: иначе
    /// страница показывает набор виджетов по умолчанию, пока кто-нибудь не
    /// поменяет настройку, и сохранённое выглядит потерянным.
    /// </summary>
    [Fact]
    public async Task OnConnectedAsync_SendsStoredConfigToCaller()
    {
        var config = new AdhdLayoutConfigDto { ShowQuiz = false };
        AdhdConfigEvent? sent = null;

        _adhdLayout
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<AdhdLayoutConfigDto>.Ok(config));
        _clients
            .Setup(client => client.AdhdConfig(It.IsAny<AdhdConfigEvent>()))
            .Callback<AdhdConfigEvent>(payload => sent = payload)
            .Returns(Task.CompletedTask);

        await CreateHub().OnConnectedAsync();

        _hubClients.Verify(clients => clients.Client("connection-1"), Times.Once);
        Assert.NotNull(sent);

        var received = JsonSerializer.Deserialize<AdhdLayoutConfigDto>(
            sent!.ConfigJson.ToStringUtf8(),
            JsonOptions
        );

        Assert.NotNull(received);
        Assert.False(received!.ShowQuiz);
    }

    /// <summary>
    /// Ошибка чтения не роняет подключение: оверлей без раскладки работает на
    /// значениях по умолчанию, а разрыв соединения был бы хуже.
    /// </summary>
    [Fact]
    public async Task OnConnectedAsync_KeepsConnectionWhenReadFails()
    {
        _adhdLayout
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                OperationResult<AdhdLayoutConfigDto>.Fail(
                    "база недоступна",
                    HttpStatusCode.ServiceUnavailable
                )
            );

        await CreateHub().OnConnectedAsync();

        _clients.Verify(client => client.AdhdConfig(It.IsAny<AdhdConfigEvent>()), Times.Never);
    }

    private OverlayHub CreateHub()
    {
        var context = new Mock<HubCallerContext>();
        context.SetupGet(caller => caller.ConnectionId).Returns("connection-1");
        context
            .SetupGet(caller => caller.ConnectionAborted)
            .Returns(TestContext.Current.CancellationToken);

        var hubContext = new Mock<IHubContext<OverlayHub, IOverlayHub>>();
        hubContext.SetupGet(context => context.Clients).Returns(_hubClients.Object);

        return new OverlayHub(
            CreateCoordinator(),
            _adhdLayout.Object,
            hubContext.Object,
            NullLogger<OverlayHub>.Instance
        )
        {
            Context = context.Object,
        };
    }

    private SoundMuteCoordinator CreateCoordinator() =>
        new(
            _audio.Object,
            _tts.Object,
            _soundRequest.Object,
            NullLogger<SoundMuteCoordinator>.Instance
        );
}
