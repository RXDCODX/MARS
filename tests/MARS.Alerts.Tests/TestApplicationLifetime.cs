using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Tests;

/// <summary>
/// Тестовый lifetime: колбэки ``ApplicationStarted`` выполняются сразу
/// (отменённый токен), поэтому подписки хендлеров активны без запуска хоста.
/// </summary>
public sealed class TestApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _started = new();

    public TestApplicationLifetime()
    {
        _started.Cancel();
    }

    public CancellationToken ApplicationStarted => _started.Token;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication() { }
}
