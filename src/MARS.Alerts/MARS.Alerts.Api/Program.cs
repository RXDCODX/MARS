using MARS.Alerts.Application;
using MARS.Alerts.Application.Alerts;
using MARS.Alerts.Application.Overlay;
using MARS.Alerts.Application.RewardInput;
using MARS.Alerts.Application.TriggerWords;
using MARS.Alerts.Application.Twitch.Messages;
using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Alerts.Application.Twitch.Rewards.Services.PyroAlerts;
using MARS.Alerts.Infrastructure.Integrations.Adhd;
using MARS.Alerts.Infrastructure.Integrations.Alerts;
using MARS.Alerts.Infrastructure.Integrations.Audio;
using MARS.Alerts.Infrastructure.Integrations.SevenTv;
using MARS.Alerts.Infrastructure.Integrations.Telegram;
using MARS.Alerts.Infrastructure.Integrations.Telegram.Configuration;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Modules.Alerts.Application.Adhd;
using MARS.Modules.Alerts.Infrastructure.Persistence.Postgres.Data;
using MARS.Modules.Alerts.Infrastructure.Persistence.Postgres.Services;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Clients.Http.Matoi;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Platform.Infrastructure.Grpc.Services;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts.SoundRequest;
using MARS.Shared.Contracts.Telegramus;
using MARS.Shared.Contracts.Tuna;
using MARS.Shared.Contracts.Voice;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Alerts.Api;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Alerts", "AlertsDb");
        builder.AddMarsWolverine(
            "MARS.Alerts",
            typeof(Program).Assembly,
            typeof(TwitchEventSubListener).Assembly,
            typeof(RewardAlertListener).Assembly,
            typeof(RewardInputMessageListener).Assembly
        );

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
        builder.Services.AddSingleton<ISoundMuteCoordinator>(sp =>
            sp.GetRequiredService<SoundMuteCoordinator>()
        );
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

        // Все награды наследуются от RewardHandlerBase. Менеджер получает события
        // погашения, обычного чата, reward-input и EventSub; общие Twitch-события
        // рассылаются подписчикам RewardHandlerBase. Каждый обработчик регистрируется один раз.
        builder.Services.AddMarsRewardHandlers();
        builder.Services.AddSingleton<RewardEventManager>();

        // Потребители RabbitMQ теперь являются Wolverine-лисенерами и
        // обнаруживаются автоматически через AddMarsWolverine. Отдельная
        // регистрация AddHostedService не требуется.
        builder.Services.AddSingleton<TriggerWordAlertDispatcher>();
        builder.Services.AddSingleton<RewardBoundAlertDispatcher>();

        // Meme workers
        builder.Services.AddHostedService<RandomMemeWorker>();
        builder.Services.AddSingleton<
            IWTelegramClientAdapterFactory,
            WTelegramClientAdapterFactory
        >();
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
