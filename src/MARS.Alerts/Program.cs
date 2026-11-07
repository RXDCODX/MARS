using MARS.Alerts.Configuration;
using MARS.Alerts.Data;
using MARS.Alerts.Hubs;
using MARS.Alerts.Models;
using MARS.Alerts.Services;
using MARS.Alerts.Services.Adhd;
using MARS.Alerts.Services.Alerts;
using MARS.Alerts.Services.Audio;
using MARS.Alerts.Services.PyroAlerts;
using MARS.Alerts.Services.RewardInput;
using MARS.Alerts.Services.Synthesizer;
using MARS.Alerts.Services.TriggerWords;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Platform.Clients.Http.Infrastructure;
using MARS.Platform.Clients.Http.Infrastructure.Matoi;
using MARS.Platform.Grpc.Infrastructure;
using MARS.Platform.Grpc.Infrastructure.Extensions;
using MARS.Platform.Grpc.Infrastructure.Notifications;
using MARS.Platform.Grpc.Infrastructure.Services;
using MARS.Platform.Hosting.Infrastructure.Extensions;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Platform.Persistence.Postgres.Infrastructure.Extensions;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.Platform.Web.AspNetCore.Infrastructure.Extensions;
using MARS.Shared.Contracts.SoundRequest;
using MARS.Shared.Contracts.Telegramus;
using MARS.Shared.Contracts.Tuna;
using MARS.Shared.Contracts.Voice;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Alerts;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Alerts", "AlertsDb");
        builder.AddMarsWolverine("MARS.Alerts", typeof(Program).Assembly);

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
        builder.Services.AddHostedService<TunaHubRelay>();
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();

        // Координатор одновременного глушения звука: микшер приложений
        // (AudioController на хосте), TTS и плеер SoundRequest.
        builder.Services.AddSingleton<SoundMuteCoordinator>();
        builder.Services.AddMarsAudioControllerClient();
        builder.Services.AddMarsGrpcClient<VoiceRecognitionService.VoiceRecognitionServiceClient>(
            builder.Configuration,
            nameof(ServiceEndpoints.TTS)
        );
        builder.Services.AddMarsGrpcClient<SoundRequestService.SoundRequestServiceClient>(
            builder.Configuration,
            nameof(ServiceEndpoints.SoundRequest)
        );

        // Триггерные алерты: список алертов читает владелец — MARS.MediaStorage.
        builder.Services.AddMarsServiceClients(builder.Configuration);
        builder.Services.AddMarsServiceClient<IMediaStorageClient, MediaStorageClient>(
            ServiceClientExtensions.MediaStorageHttpClientName
        );
        builder.Services.AddSingleton<IEnabledAlertSource, MediaStorageEnabledAlertSource>();

        // Раскладка ADHD-экрана. Владелец таблицы подменяет заглушку из
        // AddMarsGrpcHosting: в MARS.OBS её нет, и методы контракта отвечают
        // FailedPrecondition. Сервис stateless (контекст создаётся фабрикой на
        // вызов), а потребитель IAdhdConfigStore — синглтон: при scoped здесь
        // ValidateScopes роняет сборку контейнера в Development.
        builder.Services.AddSingleton<IAdhdLayoutService, AdhdLayoutService>();
        builder.Services.AddSingleton<IAdhdConfigStore, AlertsAdhdConfigStore>();

        // Configuration
        builder.Services.Configure<WTelegramConfiguration>(
            builder.Configuration.GetSection(WTelegramConfiguration.SectionName)
        );

        // Поиск постов booru для награды RANDOM ART. Клиент общий с
        // MARS.Telegram, регистрируется в MARS.Shared и сам берёт адрес и ключ
        // из секции Matoi.
        builder.Services.AddMatoiClient(builder.Configuration);

        // PyroAlerts services
        builder.Services.AddSingleton<PyroAlertsHelper>();
        builder.Services.AddSingleton<PyroAlertsHandler>();

        // Shared Twitch reward dependencies
        builder.Services.AddSingleton<RickRollerService>();
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

        // Потребители RabbitMQ теперь являются Wolverine-лисенерами и
        // обнаруживаются автоматически через AddMarsWolverine. Отдельная
        // регистрация AddHostedService не требуется.
        builder.Services.AddSingleton<TriggerWordAlertDispatcher>();
        builder.Services.AddSingleton<RewardBoundAlertDispatcher>();

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
        // Хаб информации о треке. Отдельный путь и отдельное реле: событие
        // трека едет из своего широковещателя и подписка на него своя.
        app.MapHub<TunaHub>("/hubs/tuna");
        app.MapControllers();
        app.MapGet("/", () => "MARS.Alerts is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
