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

        var result = await limiter.Permit(() => 42, TestContext.Current.CancellationToken);

        Assert.Equal(42, result);
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
