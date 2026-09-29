using MARS.Commands.Services.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Commands.Services;

/// <summary>
/// Расширения для регистрации сервисов команд
/// </summary>
public static class CommandExecutorServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет все сервисы команд в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов</returns>
    public static IServiceCollection AddCommandExecutorServices(this IServiceCollection services)
    {
        // Регистрируем фабрику команд
        services.AddSingleton<CommandFactory>();

        // Регистрируем платформенные сервисы (заглушки, не IHostedService)
        services.AddSingleton<TelegramCommandService>();
        services.AddSingleton<TwitchCommandService>();
        services.AddSingleton<DiscordCommandService>();

        // Регистрируем CommandExecutorService как Scoped
        services.AddSingleton<CommandExecutorService>();
        services.AddSingleton<ICommandService>(sp =>
            sp.GetRequiredService<CommandExecutorService>()
        );
        services.AddHostedService(sp => sp.GetRequiredService<CommandExecutorService>());

        // Регистрируем API адаптер
        services.AddSingleton<ApiCommandService>();

        return services;
    }
}
