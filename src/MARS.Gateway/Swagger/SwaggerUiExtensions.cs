using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace MARS.Gateway.Swagger;

public static class SwaggerUiExtensions
{
    public static IServiceCollection AddMarsSwaggerAggregator(this IServiceCollection services)
    {
        services.AddSingleton<SwaggerAggregatorService>();
        services.AddHostedService(sp => sp.GetRequiredService<SwaggerAggregatorService>());
        return services;
    }

    public static WebApplication UseMarsSwaggerAggregator(this WebApplication app)
    {
        var swaggerAggregator = app.Services.GetRequiredService<SwaggerAggregatorService>();

        // JSON endpoint для каждого сервиса
        app.MapGet(
            "/swagger/{serviceName}/swagger.json",
            (string serviceName, SwaggerAggregatorService aggregator) =>
                aggregator.CachedDocs.TryGetValue(serviceName, out var doc)
                    ? Results.Text(SerializeDocument(doc), "application/json")
                    : Results.NotFound()
        );

        // Отдельный документ SignalR-хабов: генератор клиента тянет его, чтобы
        // построить signalr-clients. Сервисы без хабов сюда не попадут.
        app.MapGet(
            "/swagger/{serviceName}/hubs.json",
            (string serviceName, SwaggerAggregatorService aggregator) =>
                aggregator.CachedHubsDocs.TryGetValue(serviceName, out var doc)
                    ? Results.Text(SerializeDocument(doc), "application/json")
                    : Results.NotFound()
        );

        // Swagger UI с переключателем документов
        app.UseSwaggerUI(c =>
        {
            foreach (var serviceName in swaggerAggregator.ServiceEndpoints.Keys)
            {
                c.SwaggerEndpoint($"/swagger/{serviceName}/swagger.json", $"MARS.{serviceName}");
            }
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "MARS API — All Services";
        });

        return app;
    }

    private static string SerializeDocument(OpenApiDocument document)
    {
        var writer = new StringWriter();
        document.SerializeAsV3(new OpenApiJsonWriter(writer));
        return writer.ToString();
    }
}
