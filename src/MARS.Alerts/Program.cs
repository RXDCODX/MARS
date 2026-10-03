using MARS.Alerts.Configuration;
using MARS.Alerts.Data;
using MARS.Alerts.Hubs;
using MARS.Alerts.Models;
using MARS.Alerts.Services;
using MARS.Alerts.Services.Adhd;
using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.PyroAlerts;
using MARS.Alerts.Services.RewardInput;
using MARS.Alerts.Services.Synthesizer;
using MARS.Alerts.Services.TriggerWords;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Clients;
using MARS.Shared.Extensions;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Grpc.Services;
using MARS.Shared.Grpc.Telegramus;
using MARS.Shared.Grpc.Tuna;

namespace MARS.Alerts;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Alerts", "AlertsDb");

        builder.Services.AddMarsDbContext<AlertsDbContext>(
            builder.Configuration,
            "alerts",
            "AlertsDb"
        );

        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsSignalR();
        builder.Services.AddMarsEventBroadcaster<TelegramusEvent>();
        // MARS.Alerts — владелец оверлея: держит широковещатель в памяти, отдаёт
        // подписку по gRPC (Subscribe), вызов наружу (Fire) и события в SignalR-хаб.
        builder.Services.AddSingleton<ITelegramusEventSink, BroadcasterTelegramusEventSink>();
        builder.Services.AddMarsEventBroadcaster<TunaEvent>();
        builder.Services.AddSingleton<ITelegramusNotifier, TelegramusNotifier>();
        // Реле перекладывает события из broadcaster'а в хаб оверлея. Само по себе
        // состояния не имеет, поэтому зарегистрировано как hosted service: без
        // него хаб принимал бы подключения и не получал бы ни одного события.
        builder.Services.AddHostedService<HubEventRelay>();
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();

        // Триггерные алерты: список алертов читает владелец — MARS.MediaStorage.
        builder.Services.AddMarsServiceClients(builder.Configuration);
        builder.Services.AddMarsServiceClient<IMediaStorageClient, MediaStorageClient>(
            ServiceClientExtensions.MediaStorageHttpClientName
        );
        builder.Services.AddSingleton<IEnabledAlertSource, MediaStorageEnabledAlertSource>();

        // Раскладка ADHD-экрана. Владелец таблицы подменяет заглушку из
        // AddMarsGrpcHosting: в MARS.OBS её нет, и методы контракта отвечают
        // FailedPrecondition.
        builder.Services.AddScoped<IAdhdLayoutService, AdhdLayoutService>();
        builder.Services.AddSingleton<IAdhdConfigStore, AlertsAdhdConfigStore>();

        // Configuration
        builder.Services.Configure<BooruConfiguration>(
            builder.Configuration.GetSection(BooruConfiguration.Section)
        );
        builder.Services.Configure<WTelegramConfiguration>(
            builder.Configuration.GetSection(WTelegramConfiguration.SectionName)
        );

        // PyroAlerts services
        builder.Services.AddSingleton<PyroAlertsHelper>();
        builder.Services.AddSingleton<PyroAlertsHandler>();

        // Shared Twitch reward dependencies
        builder.Services.AddSingleton<RickRollerService>();
        builder.Services.AddSingleton<DanbooruRandomPostService>();
        builder.Services.AddSingleton<HighlitedMessage>();
        builder.Services.AddSingleton<RandomMemHandler>();
        builder.Services.AddSingleton<MikuMondayTracksService>();

        // Награды. Список берётся рефлексией по реализациям IRewardAlertHandler,
        // а не перечислением: пока каждый новый хендлер нужно было ещё и дописать
        // здесь, его можно было забыть, и награда молча уходила в счётчик
        // необработанных сообщений. Разбирает их единственный RewardAlertConsumer,
        // поэтому на награду не поднимается отдельная BackgroundService со своим
        // AMQP-соединением.
        //
        // MikuMikuBeamHandler регистрируется раньше: он же IChatUserTrackingHandler,
        // и оба интерфейса должны отдавать один и тот же экземпляр.
        builder.Services.AddSingleton<MikuMikuBeamHandler>();
        builder.Services.AddSingleton<IChatUserTrackingHandler>(sp =>
            sp.GetRequiredService<MikuMikuBeamHandler>()
        );

        builder.Services.AddMarsRewardHandlers();

        // Потребители RabbitMQ: три соединения на весь сервис вместо ~30.
        builder.Services.AddHostedService<RewardAlertConsumer>();
        builder.Services.AddHostedService<SystemEventsConsumer>();
        builder.Services.AddHostedService<ChatUserConsumer>();
        builder.Services.AddHostedService<TriggerWordAlertConsumer>();
        // Диспетчер раньше не был зарегистрирован вовсе: TryAddTransient у Broadcaster
        // не покрывает конкретный тип, и hosted-сервис не мог построить граф —
        // MARS.Alerts падал на старте с «Unable to resolve service for type
        // TriggerWordAlertDispatcher». Singleton, как и сам consumer: все три
        // зависимости (IEnabledAlertSource, ITelegramusNotifier, логгер) — singleton.
        builder.Services.AddSingleton<TriggerWordAlertDispatcher>();
        builder.Services.AddHostedService<RewardInputMessageConsumer>();
        // Dispatcher-ы оверлея регистрируются явно: TryAddTransient у Broadcaster
        // покрывает только сам широковещатель, и без этих строк hosted-сервисы
        // не могли построить граф — MARS.Alerts не стартовал.
        builder.Services.AddSingleton<RewardBoundAlertDispatcher>();
        builder.Services.AddHostedService<CostMatchedRewardConsumer>();
        builder.Services.AddHostedService<TwitchMediaAlerts>();

        // Meme workers
        builder.Services.AddHostedService<RandomMemeWorker>();
        builder.Services.AddSingleton<IOnlineTelegramClientFactory, WTelegramOnlineClientFactory>();
        builder.Services.AddHostedService<RandomMemOnline>();

        // 7TV Emote service
        builder.Services.AddSingleton<ISevenTvEmoteService, SevenTvEmoteService>();
        builder.Services.AddHostedService(sp =>
            (SevenTvEmoteService)sp.GetRequiredService<ISevenTvEmoteService>()
        );

        // Worker365 и Config365 переехали в отдельный сервис MARS.Videos365
        // вместе с собственной базой mars_videos365. Здесь конвейера больше
        // нет: он публикует видео и хранит множество «уже опубликовано», а не
        // показывает алерты.

        var app = builder.Build();

        app.UseMarsDefaults();

        app.MapGrpcService<TelegramusGrpcService>();
        app.MapGrpcService<TunaGrpcService>();
        // Хаб оверлея. Путь совпадает с тем, что проксирует Gateway, и должен
        // оставаться врознь с gRPC-стримом на 8081: тот живёт по HTTP/2, а хаб
        // обслуживает браузер через :8080.
        app.MapHub<OverlayHub>("/hubs/overlay");
        app.MapControllers();
        app.MapGet("/", () => "MARS.Alerts is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
