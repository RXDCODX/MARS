using MARS.Shared.Extensions;
using MARS.Telegram.Configuration;
using MARS.Telegram.Data;
using MARS.Telegram.Services;
using MARS.Telegram.Services.BotService;
using MARS.Telegram.Services.GooglePhotos;
using MARS.Telegram.Services.PrivateChannelsResender;
using Telegram.Bot;

namespace MARS.Telegram;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Telegram", "ChatDb");

        // Configuration
        builder.Services.Configure<TelegramConfiguration>(
            builder.Configuration.GetSection(TelegramConfiguration.TelegramSection)
        );
        builder.Services.Configure<GooglePhotosConfiguration>(
            builder.Configuration.GetSection(GooglePhotosConfiguration.SectionName)
        );

        // Database
        builder.Services.AddMarsDbContext<ChatDbContext>(builder.Configuration, "chat", "ChatDb");

        // HTTP client
        builder.Services.AddHttpClient();

        // Telegram Bot Client
        var botToken =
            builder.Configuration["Telegram:BotToken"]
            ?? builder.Configuration["Telegram:Token"]
            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

        if (!string.IsNullOrEmpty(botToken))
        {
            builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
        }

        // Services
        builder.Services.AddSingleton<
            ITelegramClipboardCopyService,
            TelegramClipboardCopyService
        >();
        builder.Services.AddScoped<ITelegramDiscordBridgeService, TelegramDiscordBridgeService>();
        builder.Services.AddScoped<IGooglePhotosAuthService, GooglePhotosAuthService>();
        builder.Services.AddScoped<GooglePhotosApiClient>();
        builder.Services.AddScoped<ITelegramusService, TelegramGooglePhotosService>();
        builder.Services.AddSingleton<IWTelegramClientService, WTelegramClientService>();

        // BotService (polling)
        builder.Services.AddScoped<UpdateHandler>();
        builder.Services.AddScoped<ReceiverService>();
        builder.Services.AddHostedService<PollingService>();

        // Channel resender
        builder.Services.AddHostedService<TelegramChannelsResenderService>();

        // Controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
