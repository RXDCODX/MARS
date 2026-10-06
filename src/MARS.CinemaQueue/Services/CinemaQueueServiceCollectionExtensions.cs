using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.CinemaQueue.Services;

public static class CinemaQueueServiceCollectionExtensions
{
    /// <summary>
    /// Регистрации очереди кино — единственный их источник: <c>Program.cs</c>
    /// вызывает этот метод и ничего не дублирует.
    /// </summary>
    /// <remarks>
    /// Репозиторий и сервис — синглтоны: оба stateless, контекст создаёт
    /// <c>IDbContextFactory</c> (он сам синглтон) на каждый вызов. Регистрация
    /// <c>CinemaQueueNotificationService</c> как фоновой службы делает потребителя
    /// синглтоном, и при scoped-зависимостях в графе ValidateScopes роняет
    /// сборку контейнера в Development, а в Production отдаёт captive-объект.
    /// Остальное держится за HTTP-скоупом: раз в запрос, и состояния не несёт.
    /// </remarks>
    public static IServiceCollection AddCinemaQueueServices(this IServiceCollection services)
    {
        services.AddSingleton<ICinemaQueueRepository, CinemaQueueRepository>();
        services.AddSingleton<ICinemaQueueService, CinemaQueueService>();
        services.AddScoped<IKinopoiskService, KinopoiskService>();
        services.AddScoped<IMediaMetadataService, MediaMetadataService>();
        services.AddScoped<ITwitchCinemaQueueService, TwitchCinemaQueueService>();
        services.AddHostedService<CinemaQueueNotificationService>();

        return services;
    }
}
