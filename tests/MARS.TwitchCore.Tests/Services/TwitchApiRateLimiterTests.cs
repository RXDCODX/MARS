using MARS.TwitchCore.Services.Client;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Рейт-лимитер Twitch API: между запросами выдерживается пауза в 1,5 секунды,
/// поэтому каждый лимитер в тесте свой — иначе тест платил бы за ожидание, не
/// относящееся к проверке.
/// </summary>
public class TwitchApiRateLimiterTests
{
    [Fact]
    public async Task AsyncActionIsPerformedOnceWithToken()
    {
        var calls = 0;
        using var limiter = Limiter();

        await limiter.Perform(
            () =>
            {
                calls++;
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AsyncActionIsPerformedOnceWithoutToken()
    {
        var calls = 0;
        using var limiter = Limiter();

        // Перегрузка без токена проверяется осознанно: передача токена ушла бы в
        // соседнюю перегрузку, и проверялся бы не тот метод.
#pragma warning disable xUnit1051
        await limiter.Perform(() =>
        {
            calls++;
            return Task.CompletedTask;
        });
#pragma warning restore xUnit1051

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AsyncFunctionReturnsValueWithToken()
    {
        using var limiter = Limiter();

        var result = await limiter.Perform(
            () => Task.FromResult(42),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task AsyncFunctionReturnsValueWithoutToken()
    {
        using var limiter = Limiter();

        var result = await limiter.Perform(() => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task SynchronousActionIsPerformedWithToken()
    {
        var calls = 0;
        using var limiter = Limiter();

        await limiter.Perform(
            () =>
            {
                calls++;
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SynchronousActionIsPerformedWithoutToken()
    {
        var calls = 0;
        using var limiter = Limiter();

        // Перегрузка без токена проверяется осознанно: см. AsyncActionIsPerformedOnceWithoutToken.
#pragma warning disable xUnit1051
        await limiter.Perform(() =>
        {
            calls++;
        });
#pragma warning restore xUnit1051

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SyncFunctionReturnsValueWithToken()
    {
        using var limiter = Limiter();

        var result = await limiter.Permit(() => 42, TestContext.Current.CancellationToken);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task SyncFunctionReturnsValueWithoutToken()
    {
        using var limiter = Limiter();

        var result = await limiter.PermitWithoutToken(() => 42);

        Assert.Equal(42, result);
    }

    /// <summary>
    /// Сбой внутри функции обязателен доходит до вызывающего: иначе запрос к Twitch
    /// выглядел бы как успешный ответ с пустым значением.
    /// </summary>
    [Fact]
    public async Task FailureOfAsyncFunctionIsRethrown()
    {
        using var limiter = Limiter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            limiter.Perform(
                () => Task.FromException<int>(new InvalidOperationException("twitch отказал")),
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Сбой внутри синхронного действия тем более не должен проглатываться.
    /// </summary>
    [Fact]
    public async Task FailureOfExplicitActionIsRethrown()
    {
        using var limiter = Limiter();
        Action action = () => throw new InvalidOperationException("twitch отказал");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            limiter.Perform(action, TestContext.Current.CancellationToken)
        );
    }

    /// <summary>
    /// Между запросами выдерживается минимальный интервал: Twitch отвечает 429 при
    /// слишком частых обращениях, и после него останавливался бы весь опрос.
    /// </summary>
    [Fact]
    public async Task MinimumIntervalBetweenRequestsIsRespected()
    {
        using var limiter = Limiter();
        RememberRequest(limiter, DateTime.Now);

        var started = DateTime.Now;
        await limiter.PermitWithoutToken(() => 1);

        Assert.True(
            DateTime.Now - started >= TimeSpan.FromMilliseconds(1400),
            "Второй запрос ушёл сразу, минимальный интервал не выдержан"
        );
    }

    private static void RememberRequest(TwitchApiRateLimiter limiter, DateTime requestTime)
    {
        var queue = typeof(TwitchApiRateLimiter)
            .GetField(
                "_requestTimes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            )!
            .GetValue(limiter)!;

        ((System.Collections.Generic.Queue<DateTime>)queue).Enqueue(requestTime);
    }

    /// <summary>
    /// Исключение из запроса обязано дойти до вызывающего: проглоченная ошибка
    /// Twitch API выглядела бы как пустой успешный ответ.
    /// </summary>
    [Fact]
    public async Task FailureOfAsyncActionIsRethrown()
    {
        using var limiter = Limiter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            limiter.Perform(
                () => throw new InvalidOperationException("twitch отказал"),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task FailureOfSyncActionIsRethrown()
    {
        using var limiter = Limiter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            limiter.Permit(
                () => throw new InvalidOperationException("twitch отказал"),
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Падение запроса не должно оставлять семафор занятым: иначе один сбой
    /// Twitch API навсегда остановил бы все последующие запросы.
    /// </summary>
    [Fact]
    public async Task LimiterKeepsWorkingAfterFailure()
    {
        using var limiter = Limiter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            limiter.Perform(
                () => throw new InvalidOperationException("сбой"),
                TestContext.Current.CancellationToken
            )
        );

        var result = await limiter.Perform(
            () => Task.FromResult(7),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(7, result);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        var limiter = Limiter();

        limiter.Dispose();
        limiter.Dispose();
    }

    private static TwitchApiRateLimiter Limiter() => new(NullLogger<TwitchApiRateLimiter>.Instance);
}
