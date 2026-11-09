using MARS.Telegram.Api.Rest.Controllers;
using MARS.Telegram.Api.Rest.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Telegram.Api.Rest.Extensions;

/// <summary>Подключает и публикует REST-адаптер Telegram.</summary>
public static class TelegramRestApiExtensions
{
    public static IServiceCollection AddTelegramRestApi(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(TelegramController).Assembly);
        return services;
    }

    public static WebApplication MapTelegramRestApi(this WebApplication app)
    {
        app.MapControllers();
        app.MapWTelegramStandalonePage();
        return app;
    }
}
