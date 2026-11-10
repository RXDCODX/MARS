using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Telegram.Api.Grpc.Extensions;

/// <summary>Подключает и публикует gRPC-адаптер Telegram.</summary>
public static class TelegramGrpcApiExtensions
{
    public static IServiceCollection AddTelegramGrpcApi(this IServiceCollection services)
    {
        services.AddSingleton<TelegramAdminGrpcService>();
        services.AddSingleton<TelegramLogGrpcService>();
        return services;
    }

    public static WebApplication MapTelegramGrpcApi(this WebApplication app)
    {
        app.MapGrpcService<TelegramAdminGrpcService>();
        app.MapGrpcService<TelegramLogGrpcService>();
        return app;
    }
}
