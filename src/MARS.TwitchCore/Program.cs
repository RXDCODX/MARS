using MARS.Shared.Clients;
using MARS.Shared.Configuration;
using MARS.Shared.Extensions;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Grpc.Telegramus;
using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.AutoHello;
using MARS.TwitchCore.Services.AutoInfoFetch;
using MARS.TwitchCore.Services.AutoMessages;
using MARS.TwitchCore.Services.BlackList;
using MARS.TwitchCore.Services.ChannelRewards;
using MARS.TwitchCore.Services.Chat;
using MARS.TwitchCore.Services.Client;
using MARS.TwitchCore.Services.Connection;
using MARS.TwitchCore.Services.EventSub;
using MARS.TwitchCore.Services.HelloVideos;
using MARS.TwitchCore.Services.MiniGamesStats;
using MARS.TwitchCore.Services.PuntoSwitcher;
using MARS.TwitchCore.Services.Rewards;
using MARS.TwitchCore.Services.StreamBotNotifications;
using MARS.TwitchCore.Services.StreamManagement;
using MARS.TwitchCore.Services.Synthesizer;
using MARS.TwitchCore.Services.TekkenStreams;
using MARS.TwitchCore.Services.TwitchFollowers;
using MARS.TwitchCore.Services.UserSync;
using MARS.TwitchCore.Services.Validation;
using MARS.TwitchCore.Services.WeddingAnniversary;
using MARS.TwitchCore.Services.YouTube;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Websockets;

namespace MARS.TwitchCore;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.TwitchCore", "TwitchDb");

        // Configuration
        builder.Services.Configure<TwitchConfiguration>(
            builder.Configuration.GetSection(TwitchConfiguration.SectionName)
        );
        builder.Services.Configure<TwitchRewardsOptions>(
            builder.Configuration.GetSection(TwitchRewardsOptions.SectionName)
        );

        // DbContext
        builder.Services.AddMarsDbContext<TwitchDbContext>(
            builder.Configuration,
            "twitch",
            "TwitchDb"
        );

        // TwitchLib API
        builder.Services.AddSingleton<ITwitchAPI>(sp =>
        {
            var api = new TwitchAPI();
            api.Settings.ClientId = builder.Configuration["Twitch:ClientId"];
            api.Settings.Secret = builder.Configuration["Twitch:Secret"];
            return api;
        });

        // Rate Limiter for Twitch API
        builder.Services.AddSingleton<TwitchApiRateLimiter>();

        // EventSub WebSocket client
        builder.Services.AddSingleton<EventSubWebsocketClient>();

        // Core services
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<TokenService>());
        // Обе службы stateless: БД идёт через IDbContextFactory (синглтон), а
        // состояния у них нет. Scoped здесь означал бы captive-зависимость в
        // синглтонах FollowerDbService, LeaderboardService и у фоновых служб —
        // в Development такую сборку роняет ValidateScopes.
        builder.Services.AddSingleton<TwitchUserInfoService>();
        builder.Services.AddSingleton<ITwitchUserEnsureService, TwitchUserEnsureService>();

        // Connection manager
        builder.Services.AddSingleton<TwitchConnectionManager>();
        builder.Services.AddSingleton<ITwitchConnectionState>(sp =>
            sp.GetRequiredService<TwitchConnectionManager>()
        );
        builder.Services.AddHostedService(sp => sp.GetRequiredService<TwitchConnectionManager>());

        // Twitch client exposed through the connection manager
        builder.Services.AddSingleton<ITwitchClient>(sp =>
            sp.GetRequiredService<TwitchConnectionManager>().Client
        );

        // Validation
        builder.Services.AddSingleton<
            ITwitchEventValidationService,
            TwitchEventValidationService
        >();

        // EventSub
        builder.Services.AddSingleton<EventSubService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<EventSubService>());

        // BlackList
        builder.Services.AddHostedService<TwitchBlackListService>();

        // UserSync
        builder.Services.AddHostedService<TwitchUserSyncService>();

        // AutoMessages
        builder.Services.AddScoped<IAutoMessagesService, AutoMessagesService>();
        builder.Services.AddHostedService<AutoMessagesHandler>();

        // PuntoSwitcher
        builder.Services.AddSingleton<IPuntoSwitcherService, PuntoSwitcherService>();
        builder.Services.AddHostedService(sp =>
            (PuntoSwitcherService)sp.GetRequiredService<IPuntoSwitcherService>()
        );

        // ChannelRewards
        builder.Services.AddSingleton<ChannelRewardsService>();
        builder.Services.AddSingleton<ChannelRewardsManager>();
        builder.Services.AddHostedService<ChannelRewardsSyncService>();

        // WeddingAnniversary
        builder.Services.AddScoped<WeddingAnniversaryService>();

        // AutoInfoFetch
        builder.Services.AddHostedService<AutoRewardInfoFetcher>();

        // AudioController configuration
        builder.Services.Configure<AudioControllerOptions>(
            builder.Configuration.GetSection(AudioControllerOptions.SectionName)
        );

        // StreamBotNotifications
        builder.Services.AddHostedService<TwitchStreamStartupNotifications>();

        // YouTubeResolver
        builder.Services.AddSingleton<IYouTubeApi, YoutubeExplodeApi>();
        builder.Services.AddSingleton<YouTubeResolver>();

        // Health-check аудиоконтроллера. Аудит: клиент создавался на каждый вызов,
        // именованная регистрация даёт фабрике один handler на имя.
        builder.Services.AddHttpClient(
            "audio-controller-health",
            client => client.Timeout = TimeSpan.FromSeconds(2)
        );

        // Межсервисные HTTP-клиенты (MARS.MediaStorage, MARS.WaifuGacha)
        builder.Services.AddMarsServiceClients(builder.Configuration);
        builder.Services.AddMarsServiceClient<IMediaStorageClient, MediaStorageClient>(
            ServiceClientExtensions.MediaStorageHttpClientName
        );
        builder.Services.AddMarsServiceClient<IDiscordClient, DiscordClient>(
            ServiceClientExtensions.DiscordHttpClientName
        );
        builder.Services.AddMarsServiceClient<IWaifuGachaClient, WaifuGachaClient>(
            ServiceClientExtensions.WaifuGachaHttpClientName
        );

        // AutoHello: бизнес-логика в MARS.WaifuGacha, здесь — тонкий клиент
        // Тонкий клиент без состояния, а AutoHello — фоновая служба (синглтон).
        // Scoped означал бы captive-зависимость и падение ValidateScopes.
        builder.Services.AddSingleton<IAutoHelloService, AutoHelloClient>();
        builder.Services.AddHostedService<AutoHello>();

        // HelloVideo: алерт уходит через общий поток reward-событий в MARS.Alerts.
        // Notifier stateless — тот же случай, что и у AutoHello.
        builder.Services.AddSingleton<IHelloVideoNotifier, HelloVideoNotifier>();
        builder.Services.AddHostedService<HelloVideoWorker>();

        // Викторина: локальный файл вопросов
        builder.Services.AddSingleton<ITwitchTrivia, TwitchTriviaService>();

        // Таблица лидеров мини-игр
        builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();

        // Имя супруга по TwitchId — из MARS.WaifuGacha
        builder.Services.AddScoped<IWaifuLookupService, WaifuGachaLookupClient>();

        // Synthesizer (7TV emote service)
        builder.Services.AddSingleton<SevenTvEmoteService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<SevenTvEmoteService>());
        builder.Services.AddSingleton<ISevenTvEmoteService>(sp =>
            sp.GetRequiredService<SevenTvEmoteService>()
        );

        // TwitchFollowers
        builder.Services.AddSingleton<FollowerDbService>();
        builder.Services.AddSingleton<TwitchViewersService>();
        builder.Services.AddSingleton<RxdcodxViewersService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<RxdcodxViewersService>());
        builder.Services.AddSingleton<IRxdcodxViewersService>(sp =>
            sp.GetRequiredService<RxdcodxViewersService>()
        );

        // StreamManagement
        builder.Services.AddSingleton<TwitchStreamManagementService>();
        builder.Services.AddHostedService(sp =>
            sp.GetRequiredService<TwitchStreamManagementService>()
        );

        // Rewards
        // TwitchCore публикует события оверлея, но не владеет ими: широковещатель
        // живёт в памяти MARS.Alerts, поэтому доставка идёт наружу вызовом Fire.
        // Без этой регистрации TwitchMessagesPublisher не собирался — сервис падал
        // на старте с «Unable to resolve service for type ITelegramusNotifier».
        var endpoints = new ServiceEndpoints();
        builder.Configuration.GetSection(ServiceEndpoints.SectionName).Bind(endpoints);
        builder.Services.AddMarsGrpcClient<TelegramusService.TelegramusServiceClient>(
            endpoints.Alerts
        );
        builder.Services.AddSingleton<ITelegramusEventSink, GrpcTelegramusEventSink>();
        builder.Services.AddSingleton<ITelegramusNotifier, TelegramusNotifier>();
        builder.Services.AddHostedService<TwitchMessagesPublisher>();

        // Издатель reward-событий: единственный источник twitch.reward.*
        // для ~33 обработчиков MARS.Alerts (блокер №4)
        builder.Services.AddHostedService<RewardRedemptionPublisher>();
        builder.Services.AddHostedService<TekkenStreamsDiscordForwarderService>();

        // Отправка сообщений в чат по заявкам других сервисов: единственное
        // IRC-подключение бот-аккаунта находится здесь (блокер №19)
        builder.Services.AddHostedService<TwitchChatSendConsumer>();

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();

        app.MapControllers();
        app.MapGet("/", () => "MARS.TwitchCore is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
