using MARS.Admin.Entities;
using MARS.Admin.Services.ServiceManager;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Базовый класс управляемых сервисов: проверяется переход статусов при старте и
/// остановке, потому что именно по нему панель рисует состояние сервиса.
/// </summary>
public class ManagedServiceBaseTests
{
    [Fact]
    public async Task SuccessfulStartMarksServiceRunning()
    {
        var service = new ProbeService { StartResult = true };

        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ServiceStatus.Running, service.Status);
        Assert.NotNull(service.StartTime);
        Assert.NotNull(service.LastActivity);
        Assert.Equal(1, service.StartCalls);
    }

    /// <summary>
    /// Выключенный сервис не запускается: иначе панель включила бы то, что
    /// администратор отключил.
    /// </summary>
    [Fact]
    public async Task DisabledServiceIsNotStarted()
    {
        var service = new ProbeService { IsServiceActive = false, StartResult = true };

        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ServiceStatus.Stopped, service.Status);
        Assert.Equal(0, service.StartCalls);
    }

    [Fact]
    public async Task AlreadyRunningServiceIsNotRestarted()
    {
        var service = new ProbeService { StartResult = true };

        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, service.StartCalls);
    }

    [Fact]
    public async Task FailedStartMarksServiceError()
    {
        var service = new ProbeService { StartResult = false };

        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ServiceStatus.Error, service.Status);
        Assert.Null(service.StartTime);
    }

    [Fact]
    public async Task ThrowingStartMarksServiceErrorAndRethrows()
    {
        var service = new ProbeService { StartException = new InvalidOperationException("boom") };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(TestContext.Current.CancellationToken)
        );

        Assert.Equal(ServiceStatus.Error, service.Status);
    }

    [Fact]
    public async Task SuccessfulStopMarksServiceStopped()
    {
        var service = new ProbeService { StartResult = true, StopResult = true };

        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ServiceStatus.Stopped, service.Status);
        Assert.Equal(1, service.StopCalls);
    }

    [Fact]
    public async Task AlreadyStoppedServiceIsNotStoppedAgain()
    {
        var service = new ProbeService { StopResult = true };

        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, service.StopCalls);
    }

    /// <summary>
    /// Останавливать имеет смысл только запущенное: базовый класс отказывает
    /// остановленному сервису, поэтому сценарий готовится стартом.
    /// </summary>
    [Fact]
    public async Task FailedStopMarksServiceError()
    {
        var service = new ProbeService { StartResult = true, StopResult = false };

        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ServiceStatus.Error, service.Status);
    }

    [Fact]
    public async Task ThrowingStopMarksServiceErrorAndRethrows()
    {
        var service = new ProbeService
        {
            StartResult = true,
            StopException = new InvalidOperationException("boom"),
        };

        await service.StartAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StopAsync(TestContext.Current.CancellationToken)
        );

        Assert.Equal(ServiceStatus.Error, service.Status);
    }

    [Fact]
    public async Task LoadStateRestoresActivityAndStatusFromDatabase()
    {
        var service = new ProbeService();

        await service.LoadStateAsync(
            new ServiceState
            {
                IsServiceActive = false,
                Status = ServiceStatus.Running,
                LastActivity = new DateTime(2020, 1, 1),
            }
        );

        Assert.False(service.IsServiceActive);
        Assert.Equal(ServiceStatus.Running, service.Status);
    }

    [Fact]
    public async Task ServiceInfoMirrorsCurrentState()
    {
        var service = new ProbeService { StartResult = true };

        await service.StartAsync(TestContext.Current.CancellationToken);
        var info = service.GetServiceInfo();

        Assert.Equal("probe", info.Name);
        Assert.Equal("Probe", info.DisplayName);
        Assert.Equal("Пробный сервис", info.Description);
        Assert.Equal(ServiceStatus.Running, info.Status);
        Assert.Equal(service.StartTime, info.StartTime);
        Assert.Equal(service.LastActivity, info.LastActivity);
        Assert.True(info.IsEnabled);
    }

    /// <summary>
    /// Наследник видит Logger базового класса: без него собственные сервисы
    /// писали бы в никуда.
    /// </summary>
    [Fact]
    public void LoggerIsAvailableToDerivedService()
    {
        var service = new ProbeService();

        Assert.NotNull(service.LoggerMessage);
    }

    private sealed class ProbeService() : ManagedServiceBase(NullLogger.Instance)
    {
        public override string ServiceName => "probe";
        public override string DisplayName => "Probe";
        public override string Description => "Пробный сервис";
        public override bool IsServiceActive { get; set; } = true;

        public bool StartResult { get; init; } = true;
        public bool StopResult { get; init; } = true;
        public Exception? StartException { get; init; }
        public Exception? StopException { get; init; }
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public string? LoggerMessage => Logger.GetType().Name;

        protected override Task<bool> OnStartAsync(CancellationToken cancellationToken = default)
        {
            StartCalls++;

            if (StartException is not null)
            {
                throw StartException;
            }

            return Task.FromResult(StartResult);
        }

        protected override Task<bool> OnStopAsync(CancellationToken cancellationToken = default)
        {
            StopCalls++;

            if (StopException is not null)
            {
                throw StopException;
            }

            return Task.FromResult(StopResult);
        }
    }
}
