using System;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Telegramus;
using MARS.Twitch.Api.Configuration;
using MARS.Twitch.Api.Data;
using MARS.Twitch.Api.Services;
using MARS.Twitch.Api.Services.AutoHello;
using MARS.Twitch.Api.Services.AutoInfoFetch;
using MARS.Twitch.Api.Services.AutoMessages;
using MARS.Twitch.Api.Services.BlackList;
using MARS.Twitch.Api.Services.ChannelRewards;
using MARS.Twitch.Api.Services.Chat;
using MARS.Twitch.Api.Services.Client;
using MARS.Twitch.Api.Services.Connection;
using MARS.Twitch.Api.Services.EventSub;
using MARS.Twitch.Api.Services.HelloVideos;
using MARS.Twitch.Api.Services.Management;
using MARS.Twitch.Api.Services.MiniGamesStats;
using MARS.Twitch.Api.Services.Notifications;
using MARS.Twitch.Api.Services.PuntoSwitcher;
using MARS.Twitch.Api.Services.Rewards;
using MARS.Twitch.Api.Services.StreamBotNotifications;
using MARS.Twitch.Api.Services.StreamManagement;
using MARS.Twitch.Api.Services.Synthesizer;
using MARS.Twitch.Api.Services.TekkenStreams;
using MARS.Twitch.Api.Services.TwitchFollowers;
using MARS.Twitch.Api.Services.UserSync;
using MARS.Twitch.Api.Services.Validation;
using MARS.Twitch.Api.Services.WeddingAnniversary;
using MARS.Twitch.Api.Services.YouTube;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Websockets;

namespace MARS.Twitch.Api;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.TwitchCore", "TwitchDb");
        builder.AddMarsWolverine("MARS.TwitchCore", typeof(Program).Assembly);

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
            var api = new TwitchAPI
            {
                Settings =
                {
                    ClientId = builder.Configuration["Twitch:ClientId"],
                    Secret = builder.Configuration["Twitch:Secret"],
                },
            };
            return api;
        });

        // Rate Limiter for Twitch API
        builder.Services.AddSingleton<TwitchApiRateLimiter>();

        // EventSub WebSocket client
        builder.Services.AddSingleton<EventSubWebsocketClient>();

        // Core services
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<TokenService>());
        // Время для интервала повторной просьбы авторизации: тесту нужен свой
        // ход часов, а ждать сутки в проверке нельзя.
        builder.Services.TryAddSingleton(TimeProvider.System);
        // Адрес обработчика кода авторизации — один на ссылку и на обмен кода:
        // синглтон, потому что он читается из конфигурации и адреса сервера.
        builder.Services.AddSingleton<TwitchAuthRedirectUriProvider>();
        // Просьба пройти авторизацию уходит в MARS.Telegram — он владеет ботом.
        // Своего клиента Telegram у сервиса нет намеренно: два клиента одного токена
        // означали бы двух владельцев прав и переменные бота в двух блоках compose.
        builder.Services.AddMarsTelegramAdminClient();
        builder.Services.AddSingleton<ITwitchAuthNotifier, TelegramAuthNotifier>();
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

        // IMessageBus у Wolverine зарегистрирован как Scoped, поэтому notifier
        // также Scoped. HelloVideoWorker получает его через IServiceScopeFactory
        // отдельно для каждой отправки алерта.
        builder.Services.AddScoped<IHelloVideoNotifier, HelloVideoNotifier>();
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
        // MARS.Twitch.Api публикует события оверлея, но не владеет ими: широковещатель
        // живёт в памяти MARS.Alerts, поэтому доставка идёт наружу запросом.
        // Без этой регистрации TwitchMessagesPublisher не собирался — сервис падал
        // на старте с «Unable to resolve service for type ITelegramusNotifier».
        builder.Services.AddMarsOverlayEventsClient();

        // Издатель reward-событий теперь работает через Wolverine-лисенер,
        // регистрируемый автоматически при добавлении шины.
        builder.Services.AddHostedService<TekkenStreamsDiscordForwarderService>();

        // Отправка сообщений в чат по заявкам других сервисов теперь обрабатывается
        // Wolverine-лисенером TwitchChatSendListener, регистрируемым автоматически.
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();

        app.MapControllers();
        app.MapGet("/", () => "MARS.Twitch.Api is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        await app.RunMarsSchemaMigrationsAsync();

        await app.RunAsync();
    }
}
