using MARS.TwitchCore.Services.Client;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Синхронные перегрузки <c>Perform</c> вызываются по имени <c>Permit</c>:
/// лямбда <c>() =&gt; 42</c> компилятору подходит одинаково и под <c>Action</c>,
/// и под <c>Func&lt;int&gt;</c>, а проверять нужно именно синхронную.
/// </summary>
internal static class TwitchApiRateLimiterSync
{
    public static Task<int> Permit(
        this TwitchApiRateLimiter limiter,
        Func<int> action,
        CancellationToken cancellationToken
    ) => limiter.Perform(action, cancellationToken);

    public static Task<int> PermitAsync(
        this TwitchApiRateLimiter limiter,
        CancellationToken cancellationToken
    ) => limiter.Perform(() => Task.FromResult(42), cancellationToken);
}
