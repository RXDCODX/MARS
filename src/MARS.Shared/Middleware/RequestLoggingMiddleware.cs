using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isInfrastructureProbe = IsInfrastructureProbe(context.Request.Path);

        if (isInfrastructureProbe)
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;

        try
        {
            await _next(context);
            sw.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                method,
                path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds
            );
        }
        catch (Exception)
        {
            sw.Stop();
            _logger.LogError(
                "HTTP {Method} {Path} failed in {ElapsedMs}ms",
                method,
                path,
                sw.ElapsedMilliseconds
            );
            throw;
        }
    }

    private static bool IsInfrastructureProbe(PathString path)
    {
        return path.StartsWithSegments("/metrics") || path.StartsWithSegments("/health");
    }
}
