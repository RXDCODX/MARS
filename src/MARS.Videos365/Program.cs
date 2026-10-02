using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.Shared.Telegram;
using MARS.Videos365.Configuration;
using MARS.Videos365.Data;
using MARS.Videos365.Services;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace MARS.Videos365;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Videos365", "Videos365Db");

        builder.Services.AddMarsDbContext<Videos365DbContext>(
            builder.Configuration,
            "videos365",
            "Videos365Db"
        );

        builder.Services.AddHttpClient();

        builder.Services.Configure<Config365>(
            builder.Configuration.GetSection(Config365.SectionName)
        );

        // Проверка доступности источника и уведомление администраторов.
        // Секция Telegram — та же, что у остальных сервисов репозитория.
        var telegramBotToken =
            builder.Configuration["Telegram:BotToken"]
            ?? builder.Configuration["Telegram:Token"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

        builder.Services.Configure<TelegramConfig>(options =>
        {
            options.BotToken = telegramBotToken;

            // Список собирается вручную, а не биндером: compose подставляет
            // пустую строку для незаданного TELEGRAM_ADMIN_ID, а приведение
            // "" к long падает и роняет хост при старте.
            options.AdminIds =
            [
                .. builder
                    .Configuration.GetSection("Telegram:AdminIds")
                    .GetChildren()
                    .Select(child =>
                        long.TryParse(child.Value, out var adminId) ? adminId : (long?)null
                    )
                    .Where(adminId => adminId.HasValue)
                    .Select(adminId => adminId!.Value),
            ];
        });

        if (!string.IsNullOrEmpty(telegramBotToken))
        {
            builder.Services.AddSingleton<ITelegramBotClient>(
                new TelegramBotClient(telegramBotToken)
            );
            builder.Services.AddSingleton<ITelegramAdminMessenger, TelegramAdminMessenger>();
        }

        builder.Services.AddSingleton<IDnsResolver, SystemDnsResolver>();
        builder.Services.AddScoped<SiteAvailabilityChecker>();

        // Клиент Telegram регистрируется только когда задан токен, поэтому
        // уведомитель собирается вручную: так пустой токен даёт воркеру
        // работающий сервис с предупреждением в логе, а не падение контейнера.
        builder.Services.AddScoped<SiteUnavailableNotifier>(sp =>
            new(
                sp.GetService<ITelegramAdminMessenger>(),
                sp.GetRequiredService<IOptions<TelegramConfig>>(),
                sp.GetRequiredService<ILogger<SiteUnavailableNotifier>>()
            )
        );

        builder.Services.AddHostedService<Worker365>();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapGet("/", () => "MARS.Videos365 is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
