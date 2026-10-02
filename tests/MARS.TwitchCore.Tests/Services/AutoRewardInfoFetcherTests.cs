using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.AutoInfoFetch;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Периодическое обновление сведений о наградах канала.
///
/// Класс сознательно «не падает»: и стартовая загрузка, и срабатывание таймера
/// ловят исключение внутри себя. Поэтому проверяется именно это — обход Twitch
/// не должен валить фоновый сервис, иначе сведения о наградах замирают молча.
/// </summary>
public class AutoRewardInfoFetcherTests
{
    [Fact]
    public async Task StartDoesNotThrowOnUnavailableApi()
    {
        using var service = Create(Mock.Of<ITwitchAPI>());

        await service.StartAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Повторный запуск на остановленном токене не ходит в Twitch: после
    /// остановки хоста фоновые обходы Twitch лишние.
    /// </summary>
    [Fact]
    public async Task CancelledTokenSkipsFetch()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var service = Create(Mock.Of<ITwitchAPI>());

        await service.StartAsync(cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
    }

    /// <summary>
    /// Срабатывание таймера после остановки тоже игнорируется: сервис мог бы
    /// начать запрос и упасть на отменённом токене.
    /// </summary>
    [Fact]
    public async Task TimerElapsedAfterStopIsSafe()
    {
        using var cancellation = new CancellationTokenSource();
        using var service = Create(Mock.Of<ITwitchAPI>());

        await service.StartAsync(cancellation.Token);
        await cancellation.CancelAsync();

        await InvokeTimerAsync(service);
    }

    [Fact]
    public async Task DisposeIsIdempotent()
    {
        var service = Create(Mock.Of<ITwitchAPI>());

        await service.StartAsync(TestContext.Current.CancellationToken);
        service.Dispose();
        service.Dispose();
    }

    /// <summary>
    /// Обработчик таймера — async void, поэтому возвращаемого Task у него нет:
    /// ждать десять минут ради одного срабатывания тест не может, а на
    /// отменённом токене тело метода выполняется синхронно и ничего не ждёт.
    /// </summary>
    private static Task InvokeTimerAsync(AutoRewardInfoFetcher service)
    {
        var method = typeof(AutoRewardInfoFetcher).GetMethod(
            "OnTimerElapsed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;

        method.Invoke(service, [null, null]);

        return Task.CompletedTask;
    }

    private static AutoRewardInfoFetcher Create(ITwitchAPI api) =>
        new(
            api,
            new TokenService(
                Mock.Of<ITwitchAPI>(),
                NullLogger<TokenService>.Instance,
                new TwitchTestDbContextFactory()
            ),
            NullLogger<AutoRewardInfoFetcher>.Instance
        );
}
