using MARS.Shared.Extensions;
using MARS.Telegram.Configuration;
using MARS.Telegram.Data;
using MARS.Telegram.Endpoints;
using MARS.Telegram.Grpc;
using MARS.Telegram.Services;
using MARS.Telegram.Services.Booru;
using MARS.Telegram.Services.BotService;
using MARS.Telegram.Services.GooglePhotos;
using MARS.Telegram.Services.Messaging;
using MARS.Telegram.Services.PrivateChannelsResender;
using MARS.Telegram.Services.WTelegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Telegram;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Telegram", "ChatDb");

        // gRPC для просьб от других сервисов: единственный владелец бота получает
        // через него сообщения администраторам (сейчас — MARS.TwitchCore с просьбой
        // пройти переавторизацию Twitch). Без этого каждый сервис, которому
        // понадобился бы админ, заводил бы своего клиента Telegram.
        builder.AddMarsGrpcHosting();

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

        // Бот заходит по MTProto: прямой доступ к api.telegram.org с машины
        // владельца закрыт, поэтому клиент идёт через локальный прокси, а токен
        // бота авторизует его внутри протокола. Клиент Bot API здесь больше не
        // нужен: он ходил по HTTPS и с этой машины не отвечал вовсе.

        // Services
        builder.Services.AddScoped<ITelegramusService, TelegramGooglePhotosService>();
        builder.Services.AddSingleton<IWTelegramClientService, WTelegramClientService>();
        // Клиент WTelegram для бота и пользовательского аккаунта: учётные данные
        // приложения, прокси и сессия в БД. Один на весь сервис — вход дорогой, а
        // два клиента на один аккаунт Telegram расходятся по сессиям.
        builder.Services.AddSingleton<IWTelegramClientFactory, WTelegramClientFactory>();

        // Бот: сессия с входом и приёмом обновлений, отправитель сообщений.
        // Регистрируются всегда: без токена вход не состоится, но об этом должна
        // сказать служба, а не DI-контейнер сообщением «Unable to resolve service».
        builder.Services.AddSingleton<ITelegramBotSession, TelegramBotSession>();
        builder.Services.AddSingleton<ITelegramMessageSender, MTProtoMessageSender>();

        // Обработчики входящих сообщений. Каждый зарегистрирован и как
        // самостоятельная служба, и как обработчик: список один на всех, иначе он
        // оказался бы пустым и ни одного сообщения никто бы не увидел.
        builder.Services.AddSingleton<
            ITelegramClipboardCopyService,
            TelegramClipboardCopyService
        >();
        builder.Services.AddSingleton<ITelegramIncomingHandler>(sp =>
            sp.GetRequiredService<ITelegramClipboardCopyService>()
        );

        builder.Services.AddScoped<ITelegramDiscordBridgeService, TelegramDiscordBridgeService>();
        builder.Services.AddScoped<IGooglePhotosAuthService, GooglePhotosAuthService>();
        builder.Services.AddScoped<GooglePhotosApiClient>();
        builder.Services.AddScoped<ITelegramusService, TelegramGooglePhotosService>();
        builder.Services.AddScoped<ITelegramIncomingHandler>(sp =>
            sp.GetRequiredService<ITelegramusService>()
        );

        // Пересылка сообщений из приватных каналов: тоже MTProto, через
        // пользовательский клиент, а не через ботский опрос.
        builder.Services.AddHostedService<TelegramChannelsResenderService>();

        // Автопостинг booru: выборка постов, дедупликация и сверка расписания.
        // Клиента поиска постов здесь нет: единственный потребитель booru —
        // награда RANDOM ART в MARS.Alerts, а автопостинг в этом сервисе не
        // реализован (таблицы есть, публикаторов нет). Регистрировать клиент
        // matoi в пустоту незачем — DI-регистрация без потребителя читается как
        // «функция включена», а её нет.
        builder.Services.AddSingleton<IDeduplicationService, DeduplicationService>();

        // Приёмник просьб от соседних сервисов.
        builder.Services.AddSingleton<TelegramAdminGrpcService>();

        // Controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();

        // Приём сообщений администраторам от соседних сервисов. Регистрируется
        // всегда: без токена запрос должен получать внятный отказ, а не падать у
        // вызывающего на «Unable to resolve service».
        app.MapGrpcService<TelegramAdminGrpcService>();

        // Минимальная страница входа в учётную запись Telegram на случай, когда
        // служба запущена отдельно и сайта с формой кода нет. Отдаётся на любой
        // неописанный адрес: опечатка приводит сюда же, а не в 404 без подсказки.
        // Описаны адреса API и gRPC остаются своими — запасной маршрут последний.
        app.MapWTelegramStandalonePage();

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
