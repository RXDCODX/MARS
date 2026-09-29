using Microsoft.OpenApi.Writers;
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
        app.MapGet("/swagger/{serviceName}/swagger.json", async (
            string serviceName,
            SwaggerAggregatorService aggregator) =>
        {
            if (aggregator.CachedDocs.TryGetValue(serviceName, out var doc))
            {
                var writer = new StringWriter();
                var jsonWriter = new OpenApiJsonWriter(writer);
                doc.SerializeAsV3(jsonWriter);
                return Results.Text(writer.ToString(), "application/json");
            }
            return Results.NotFound();
        });

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
}
