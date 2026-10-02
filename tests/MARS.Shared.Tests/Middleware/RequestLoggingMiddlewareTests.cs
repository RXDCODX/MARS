using MARS.Shared.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Shared.Tests.Middleware;

/// <summary>
/// Логирование запросов: ошибка обязана попасть в лог ДО того, как исключение
/// уйдёт дальше, иначе в Loki не осталось бы следа упавшего запроса.
///
/// Проба инфраструктуры (<c>/metrics</c>, <c>/health</c>) не логируется: Prometheus
/// опрашивает их постоянно, и каждая строка лога была бы шумом поверх реальных
/// запросов.
/// </summary>
public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task SuccessfulRequestPassesThrough()
    {
        var (context, calls) = await Invoke("/api/Media/info", _ => Task.CompletedTask);

        Assert.Equal(1, calls);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task FailingRequestIsRethrown()
    {
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Invoke(
                "/api/Media/info",
                _ =>
                {
                    calls++;
                    throw new InvalidOperationException("сбой");
                }
            )
        );

        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("/metrics")]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task InfrastructureProbesAreNotLogged(string path)
    {
        var (_, calls) = await Invoke(path, _ => Task.CompletedTask);

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task InfrastructureProbeFailureIsNotSwallowed()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Invoke("/health", _ => throw new InvalidOperationException("проба упала"))
        );
    }

    private static async Task<(HttpContext Context, int Calls)> Invoke(
        string path,
        Func<HttpContext, Task> next
    )
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";

        var calls = 0;
        var middleware = new RequestLoggingMiddleware(
            async ctx =>
            {
                calls++;
                await next(ctx);
            },
            NullLogger<RequestLoggingMiddleware>.Instance
        );

        await middleware.InvokeAsync(context);

        return (context, calls);
    }
}
