using MARS.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Shared.Extensions;

/// <summary>
/// Настройка секции <c>Telegram</c> одинаково для всех сервисов.
/// </summary>
/// <remarks>
/// Сам <c>ITelegramBotClient</c> регистрирует вызывающий сервис: пакет
/// Telegram.Bot не должен попадать в MARS.Shared, который ссылается
/// буквально все проекты репозитория.
/// </remarks>
public static class TelegramConfigurationExtensions
{
    public static string? ResolveTelegramBotToken(this IConfiguration configuration)
    {
        return configuration["Telegram:BotToken"]
            ?? configuration["Telegram:Token"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
    }

    public static IServiceCollection AddMarsTelegramOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var botToken = configuration.ResolveTelegramBotToken();

        services.Configure<TelegramConfig>(options =>
        {
            options.BotToken = botToken;

            // Список собирается вручную, а не биндером: compose подставляет
            // пустую строку для незаданного TELEGRAM_ADMIN_ID, а приведение
            // "" к long падает и роняет хост при старте.
            options.AdminIds =
            [
                .. configuration
                    .GetSection("Telegram:AdminIds")
                    .GetChildren()
                    .Select(child =>
                        long.TryParse(child.Value, out var adminId) ? adminId : (long?)null
                    )
                    .Where(adminId => adminId.HasValue)
                    .Select(adminId => adminId!.Value),
            ];
        });

        return services;
    }
}
