using MARS.Shared.Extensions;
using MARS.Telegram.Configuration;
using MARS.Telegram.Data;
using MARS.Telegram.Services;
using MARS.Telegram.Services.Booru;
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

        // Опрашивать Telegram имеет смысл только с токеном.
        //
        // PollingService разрешает ReceiverService из области на каждом проходе, а
        // ReceiverService требует ITelegramBotClient, который выше зарегистрирован
        // только при непустом токене. Без токена каждый проход падал с
        // «Unable to resolve service», и в журнал уходила ошибка раз в пять
        // секунд на протяжении всей работы сервиса — то есть настоящая поломка
        // была завалена повторениями и переставала быть видна.
        var telegramConfigured = !string.IsNullOrEmpty(botToken);

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

        // BotService (polling).
        //
        // Только при заданном токене: без него опрашивать нечего, а ReceiverService
        // всё равно не собирается — см. замечание про telegramConfigured выше.
        //
        // Второй опрашиватель, TelegramChannelsResenderService, тоже требует клиента,
        // и на то же условие. Без токена оба молчат, а сервис поднимается.
        if (telegramConfigured)
        {
            builder.Services.AddScoped<UpdateHandler>();
            builder.Services.AddScoped<ReceiverService>();
            builder.Services.AddHostedService<PollingService>();

            // Channel resender
            builder.Services.AddHostedService<TelegramChannelsResenderService>();
        }

        // Автопостинг booru: выборка постов, дедупликация и сверка расписания.
        // Клиента поиска постов здесь нет: единственный потребитель booru —
        // награда RANDOM ART в MARS.Alerts, а автопостинг в этом сервисе не
        // реализован (таблицы есть, публикаторов нет). Регистрировать клиент
        // matoi в пустоту незачем — DI-регистрация без потребителя читается как
        // «функция включена», а её нет.
        builder.Services.AddSingleton<IDeduplicationService, DeduplicationService>();

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
