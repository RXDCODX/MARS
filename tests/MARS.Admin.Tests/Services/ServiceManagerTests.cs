using System.Net;
using MARS.Admin.Data;
using MARS.Admin.Entities;
using MARS.Admin.Services.ServiceManager;
using MARS.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Реестр сервисов админки: проверка состояния по health-check'ам и работа с
/// активностью. Сеть не поднимается — HttpClient получает заглушку обработчика,
/// поэтому проверяется логика менеджера, а не доступность Docker-сети.
/// </summary>
public class ServiceManagerTests
{
    [Fact]
    public async Task RegistryContainsEveryConfiguredService()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var services = (await manager.GetAllServicesAsync()).ToArray();

        Assert.Equal(12, services.Length);
        Assert.All(services, service => Assert.NotNull(service.HealthEndpoint));
        Assert.All(services, service => Assert.EndsWith("/health", service.HealthEndpoint));
        Assert.Contains(services, service => service.Name == "twitch-core");
        Assert.Contains(services, service => service.Name == "media-storage");
    }

    [Fact]
    public async Task HealthEndpointIsBuiltFromServiceEndpoints()
    {
        var endpoints = new ServiceEndpoints { TwitchCore = "http://my-twitch:1234" };

        var manager = Create(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)),
            endpoints
        );
        var info = await manager.GetServiceInfoAsync("twitch-core");

        Assert.NotNull(info);
        Assert.Equal("http://my-twitch:1234/health", info!.HealthEndpoint);
    }

    [Fact]
    public async Task HealthyServiceIsMarkedRunningWithStartTime()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var info = await manager.GetServiceInfoAsync("obs");

        Assert.Equal(ServiceStatus.Running, info!.Status);
        Assert.NotNull(info.LastActivity);
        Assert.NotNull(info.StartTime);
    }

    [Fact]
    public async Task UnhealthyServiceIsMarkedError()
    {
        var manager = Create(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))
        );

        var info = await manager.GetServiceInfoAsync("obs");

        Assert.Equal(ServiceStatus.Error, info!.Status);
        Assert.Null(info.StartTime);
        Assert.NotNull(info.LastActivity);
    }

    [Fact]
    public async Task UnreachableServiceIsMarkedStopped()
    {
        var manager = Create(
            new StubHandler(_ => throw new HttpRequestException("connection refused"))
        );

        var info = await manager.GetServiceInfoAsync("obs");

        Assert.Equal(ServiceStatus.Stopped, info!.Status);
    }

    /// <summary>
    /// Таймаут health-check'а приходит как <see cref="TaskCanceledException"/>:
    /// зависший сервис должен показываться как ошибочный, а не как остановленный,
    /// иначе в панели он неотличим от выключенного намеренно.
    /// </summary>
    [Fact]
    public async Task TimedOutServiceIsMarkedError()
    {
        var manager = Create(new StubHandler(_ => throw new TaskCanceledException("timeout")));

        var info = await manager.GetServiceInfoAsync("obs");

        Assert.Equal(ServiceStatus.Error, info!.Status);
    }

    [Fact]
    public async Task UnexpectedHealthCheckFailureIsMarkedError()
    {
        var manager = Create(new StubHandler(_ => throw new InvalidOperationException("boom")));

        var info = await manager.GetServiceInfoAsync("obs");

        Assert.Equal(ServiceStatus.Error, info!.Status);
    }

    [Fact]
    public async Task StatusesAreCollectedForEveryService()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var statuses = await manager.GetServicesStatusAsync();

        Assert.Equal(12, statuses.Count);
        Assert.All(statuses.Values, status => Assert.Equal(ServiceStatus.Running, status));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UnknownOrBlankServiceNameYieldsNoInfo(string? serviceName)
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Null(await manager.GetServiceInfoAsync(serviceName!));
    }

    [Fact]
    public async Task UnknownServiceNameYieldsNoInfo()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Null(await manager.GetServiceInfoAsync("nope"));
    }

    /// <summary>
    /// Сервис без health-endpoint не опрашивается: без адреса опрос упал бы
    /// исключением, и один битый сервис уронил бы весь список.
    /// </summary>
    [Fact]
    public async Task ServiceWithoutHealthEndpointStaysUnknown()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var manager = Create(handler);
        var info = await manager.GetServiceInfoAsync("obs");
        info!.HealthEndpoint = "   ";

        var statuses = await manager.GetServicesStatusAsync();

        Assert.Equal(ServiceStatus.Unknown, statuses["obs"]);
    }

    /// <summary>
    /// Админка не управляет контейнерами: Docker живёт в compose, поэтому
    /// старт и стоп обязаны честно вернуть false, а не «успех».
    /// </summary>
    [Fact]
    public async Task StartAndStopAreNotSupported()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.False(await manager.StartServiceAsync("obs"));
        Assert.False(await manager.StopServiceAsync("obs"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RestartOfBlankNameFails(string? serviceName)
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.False(await manager.RestartServiceAsync(serviceName!));
    }

    /// <summary>
    /// Перезапуск не может состояться, раз стоп возвращает false: иначе метод
    /// рапортовал бы об успехе, не сделав ничего.
    /// </summary>
    [Fact]
    public async Task RestartFailsWhenStopFails()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.False(await manager.RestartServiceAsync("obs"));
    }

    [Fact]
    public async Task ActiveStateIsPersistedForKnownService()
    {
        var (manager, factory) = CreateWithFactory(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))
        );
        await using (
            var seed = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            seed.ServiceStates.Add(
                new ServiceState
                {
                    ServiceName = "obs",
                    IsServiceActive = true,
                    Status = ServiceStatus.Stopped,
                }
            );
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.True(await manager.SetServiceActiveAsync("obs", false));

        await using var verify = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var state = await verify.ServiceStates.SingleAsync(TestContext.Current.CancellationToken);
        Assert.False(state.IsServiceActive);
    }

    [Fact]
    public async Task ActiveStateIsMarkedInRegistryEvenWithoutDbRow()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.True(await manager.SetServiceActiveAsync("obs", false));
        var info = await manager.GetServiceInfoAsync("obs");
        Assert.False(info!.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ActiveStateOfBlankNameIsRejected(string? serviceName)
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.False(await manager.SetServiceActiveAsync(serviceName!, true));
    }

    [Fact]
    public async Task ActiveStateOfUnknownServiceIsRejected()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.False(await manager.SetServiceActiveAsync("nope", true));
    }

    /// <summary>
    /// Упавшая база не должна ломать ответ: активность уже применена в реестре,
    /// а ошибка персиста уходит в лог.
    /// </summary>
    [Fact]
    public async Task ActiveStateSurvivesDatabaseFailure()
    {
        var factory = new Mock<IDbContextFactory<AdminDbContext>>();
        factory
            .Setup(instance => instance.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var manager = Create(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)),
            dbContextFactory: factory.Object
        );

        Assert.True(await manager.SetServiceActiveAsync("obs", false));
    }

    [Fact]
    public async Task LogsAreGeneratedForRequestedService()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var logs = (await manager.GetServiceLogsAsync("obs", 100)).ToArray();

        Assert.Equal(10, logs.Length);
        Assert.All(logs, log => Assert.Contains("obs", log.Message));
        Assert.Equal(
            ["Error", "Info", "Warning", "Error", "Warning", "Info"],
            logs.Take(6).Select(log => log.Level).ToArray()
        );
        Assert.True(logs[0].Timestamp > logs[1].Timestamp, "Свежая строка должна идти первой.");
    }

    /// <summary>
    /// Больше десяти строк логов сервис не отдаёт: это заглушка, а не источник
    /// логов, и лимит держит ответ предсказуемым.
    /// </summary>
    [Fact]
    public async Task LogCountIsCappedAtTen()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Equal(3, (await manager.GetServiceLogsAsync("obs", 3)).Count());
        Assert.Equal(10, (await manager.GetServiceLogsAsync("obs", 1000)).Count());
    }

    [Theory]
    [InlineData("obs", 0)]
    [InlineData("", 5)]
    [InlineData("   ", 5)]
    public async Task LogsAreEmptyForBlankNameOrZeroCount(string serviceName, int count)
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Empty(await manager.GetServiceLogsAsync(serviceName, count));
    }

    [Fact]
    public async Task ExceptionColumnIsFilledEveryFifthRow()
    {
        var manager = Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var logs = (await manager.GetServiceLogsAsync("obs")).ToArray();

        Assert.Equal("Exception 0", logs[0].Exception);
        Assert.Null(logs[1].Exception);
        Assert.Equal("Exception 5", logs[5].Exception);
    }

    private static ServiceManager Create(
        HttpMessageHandler handler,
        ServiceEndpoints? endpoints = null,
        IDbContextFactory<AdminDbContext>? dbContextFactory = null
    ) => CreateWithFactory(handler, endpoints, dbContextFactory).Manager;

    private static (
        ServiceManager Manager,
        IDbContextFactory<AdminDbContext> Factory
    ) CreateWithFactory(
        HttpMessageHandler handler,
        ServiceEndpoints? endpoints = null,
        IDbContextFactory<AdminDbContext>? dbContextFactory = null
    )
    {
        var factory = dbContextFactory ?? new AdminDbContextFactory();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient("HealthCheck"))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        var manager = new ServiceManager(
            httpClientFactory.Object,
            NullLogger<ServiceManager>.Instance,
            factory,
            Options.Create(endpoints ?? new ServiceEndpoints())
        );

        return (manager, factory);
    }

    /// <summary>
    /// Обработчик без сети: тест проверяет реакцию менеджера на ответ, а не
    /// доступность реального сервиса.
    /// </summary>
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request.RequestUri?.ToString() ?? string.Empty);
            var response = responder(request);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}
