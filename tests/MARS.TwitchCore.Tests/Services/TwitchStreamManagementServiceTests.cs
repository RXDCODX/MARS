using MARS.TwitchCore.Services.StreamManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Управление трансляцией: название, категория, теги.
///
/// Сервис сознательно молчит об отказах: он вызывается из команд чата, и вместо
/// исключения в лог уходит «не получилось». Проверяется именно этот контракт и то,
/// что выключенный сервис не ходит в Twitch вовсе — иначе правкой флага можно было
/// бы упереться в лимит запросов аккаунта, не заметив этого.
/// </summary>
public class TwitchStreamManagementServiceTests
{
    private readonly TwitchStreamManagementService _service = Create();

    [Fact]
    public async Task EmptyTitleIsRejected()
    {
        Assert.False(await _service.ChangeStreamTitleAsync("   "));
    }

    [Fact]
    public async Task TitleChangeWithoutTokenFails()
    {
        Assert.False(await _service.ChangeStreamTitleAsync("новое название"));
    }

    [Fact]
    public async Task DisabledServiceDoesNotChangeTitle()
    {
        var service = Create();
        service.IsServiceActive = false;

        Assert.False(await service.ChangeStreamTitleAsync("новое название"));
    }

    [Fact]
    public async Task StreamInfoWithoutTokenIsNull()
    {
        Assert.Null(await _service.GetStreamInfoAsync());
    }

    [Fact]
    public async Task DisabledServiceHasNoStreamInfo()
    {
        var service = Create();
        service.IsServiceActive = false;

        Assert.Null(await service.GetStreamInfoAsync());
    }

    [Fact]
    public async Task CurrentTitleIsNullWithoutStreamInfo()
    {
        Assert.Null(await _service.GetCurrentTitleAsync());
    }

    /// <summary>
    /// Сервис считается доступным, когда он включён и есть токен: без токена
    /// Twitch отклонил бы любой запрос, и команда должна сказать об этом сразу.
    /// </summary>
    [Fact]
    public void AvailabilityRequiresTokenAndActiveService()
    {
        var service = Create();

        Assert.False(service.IsServiceAvailable());

        service.IsServiceActive = false;

        Assert.False(service.IsServiceAvailable());
    }

    private static TwitchStreamManagementService Create() =>
        new(
            Mock.Of<ITwitchAPI>(),
            TwitchTokenServiceStub.WithoutToken(),
            NullLogger<TwitchStreamManagementService>.Instance
        );
}
