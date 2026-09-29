using MARS.Gateway.Swagger;
using MARS.Shared.Extensions;

namespace MARS.Gateway;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Gateway");

        // YARP
        builder
            .Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("Yarp"));

        // HTTP client for the Swagger aggregator service
        builder.Services.AddHttpClient();

        // Swagger aggregator
        builder.Services.AddMarsSwaggerAggregator();

        var app = builder.Build();

        // Собственный Swagger-UI Gateway отключаем: его место занимает агрегатор
        // (список всех сервисов). Два UseSwaggerUI на /swagger конфликтуют.
        app.UseMarsDefaults(includeSwagger: false);

        app.UseMarsSwaggerAggregator();
        app.MapReverseProxy();

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
