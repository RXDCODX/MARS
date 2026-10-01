using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MARS.Shared.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddMarsTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName
    )
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource
                    .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
                    .AddAttributes(
                        new Dictionary<string, object>
                        {
                            ["deployment.environment"] =
                                configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                            ["service.instance.id"] = Environment.MachineName,
                        }
                    )
            )
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("MARS.*")
                    .AddOtlpExporter(options =>
                    {
                        var endpoint = configuration["Otlp:Endpoint"];
                        if (!string.IsNullOrEmpty(endpoint))
                            options.Endpoint = new Uri(endpoint);
                    });
            });
        // Метрики намеренно идут через prometheus-net, а не через OTel.
        //
        // Раньше здесь стоял WithMetrics(...) с AddAspNetCoreInstrumentation и
        // AddHttpClientInstrumentation, но БЕЗ экспортёра: ни AddPrometheusExporter,
        // ни OTLP-метрик. Инструменты от метров Microsoft.AspNetCore.Hosting и
        // Microsoft.Extensions.Http создавались, собирались и никуда не уходили.
        // Кастомные метрики MarsMetrics до моста тоже не доходили: мост фильтрует
        // по Meter.Name.StartsWith("MARS.") и OTel-инструменты этому условию не
        // удовлетворяют.
        //
        // Добавление OTel-экспортёра продублировало бы HTTP-метрики prometheus-net
        // (UseHttpMetrics в UseMarsDefaults) и заставило бы переписать PromQL в
        // дашборде mars-overview на другой формат имён. Поэтому OTel отвечает
        // только за трейсы, метрики — prometheus-net плюс мост ниже.

        services.AddSingleton<OpenTelemetryPrometheusBridge>();

        return services;
    }
}
