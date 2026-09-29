using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MARS.Shared.Extensions;

/// <summary>
/// Internal HTTP endpoints для межсервисного общения.
/// Каждый сервис регистрирует свои internal endpoints через этот helper.
/// </summary>
public static class InternalEndpointsExtensions
{
    /// <summary>
    /// Регистрирует базовые internal endpoints (/internal/health, /internal/info).
    /// </summary>
    public static IEndpointRouteBuilder MapMarsInternalEndpoints(
        this IEndpointRouteBuilder endpoints,
        string serviceName
    )
    {
        endpoints.MapGet(
            "/internal/health",
            () =>
                Results.Ok(
                    new
                    {
                        service = serviceName,
                        status = "healthy",
                        timestamp = DateTime.UtcNow,
                    }
                )
        );

        endpoints.MapGet(
            "/internal/info",
            () => Results.Ok(new { service = serviceName, version = "1.0.0" })
        );

        return endpoints;
    }
}
