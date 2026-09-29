using MARS.Shared.Authentication;
using MARS.Shared.Middleware;
using MARS.Shared.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace MARS.Shared.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Стандартный pipeline для всех MARS сервисов.
    /// </summary>
    /// <param name="includeSwagger">
    /// false отключает собственный Swagger-UI сервиса. Нужен Gateway: там
    /// используется агрегатор (<c>UseMarsSwaggerAggregator</c>), а два
    /// UseSwaggerUI на префиксе /swagger конфликтуют — побеждает первый
    /// зарегистрированный, и список сервисов не отображается.
    /// </param>
    public static WebApplication UseMarsDefaults(
        this WebApplication app,
        bool includeSwagger = true
    )
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseMiddleware<CorrelationIdMiddleware>();

        if (includeSwagger)
        {
            app.UseSwaggerInAllEnvironments();
        }

        app.UseCors("CorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHttpMetrics();
        app.UseMetricServer("/metrics");

        _ = app.Services.GetRequiredService<OpenTelemetryPrometheusBridge>();

        app.MapHealthChecks("/health");
        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }
        );
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

        return app;
    }

    /// <summary>
    /// Swagger доступен во всех окружениях: агрегатор в Gateway и compose-стек
    /// полагаются на него в Production.
    /// </summary>
    private static WebApplication UseSwaggerInAllEnvironments(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }

    /// <summary>
    /// Регистрирует маршруты и кластеры, доступные только с корректным API-ключом.
    /// </summary>
    public static void RequireServiceApiKey(this RouteHandlerBuilder builder)
    {
        builder.RequireAuthorization(ServiceAuthExtensions.PolicyName);
    }
}
