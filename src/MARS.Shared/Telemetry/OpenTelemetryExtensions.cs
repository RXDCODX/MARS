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
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("MARS.*");
            });

        services.AddSingleton<OpenTelemetryPrometheusBridge>();

        return services;
    }
}
