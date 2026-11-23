using System;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Twitch.Application.Configuration;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.AutoHello;
using MARS.Twitch.Infrastructure.Services.AutoMessages;
using MARS.Twitch.Infrastructure.Services.BlackList;
using MARS.Twitch.Infrastructure.Services.ChannelRewards;
using MARS.Twitch.Infrastructure.Services.Chat;
using MARS.Twitch.Infrastructure.Services.Client;
using MARS.Twitch.Infrastructure.Services.Commands;
using MARS.Twitch.Infrastructure.Services.Connection;
using MARS.Twitch.Infrastructure.Services.EventSub;
using MARS.Twitch.Infrastructure.Services.HelloVideos;
using MARS.Twitch.Infrastructure.Services.Management;
using MARS.Twitch.Infrastructure.Services.MiniGamesStats;
using MARS.Twitch.Infrastructure.Services.PuntoSwitcher;
using MARS.Twitch.Infrastructure.Services.Rewards;
using MARS.Twitch.Infrastructure.Services.StreamBotNotifications;
using MARS.Twitch.Infrastructure.Services.StreamManagement;
using MARS.Twitch.Infrastructure.Services.Synthesizer;
using MARS.Twitch.Infrastructure.Services.TekkenStreams;
using MARS.Twitch.Infrastructure.Services.TwitchFollowers;
using MARS.Twitch.Infrastructure.Services.UserSync;
using MARS.Twitch.Infrastructure.Services.Validation;
using MARS.Twitch.Infrastructure.Services.WeddingAnniversary;
using MARS.Twitch.Infrastructure.Services.YouTube;
using MARS.WaifuGacha.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Websockets;

namespace MARS.Twitch.Host;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Twitch", "TwitchDb");
        builder.AddMarsWolverine(
            "MARS.Twitch",
            typeof(Program).Assembly,
            typeof(TwitchChatSendListener).Assembly
        );

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

        // Публикатор событий чата: ChatEventMessageEvent / RewardInputChatMessageEvent /
        // ChatMessageDeletedEvent. Потерян в ветке микросервисов — без него
        // ChatHorizontal/ChatVertical/NewMessage и DeleteMessage молчат.
        builder.Services.AddSingleton<TwitchMessagesPublisher>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<TwitchMessagesPublisher>());

        // Пересылка команд (!...) в MARS.Commands через CommandExecuteRequestedEvent.
        // Потеряна в ветке микросервисов — без неё команды из Twitch не выполняются.
        builder.Services.AddSingleton<TwitchCommandForwarderService>();
        builder.Services.AddHostedService(sp =>
            sp.GetRequiredService<TwitchCommandForwarderService>()
        );

        // Validation
        builder.Services.AddSingleton<
            ITwitchEventValidationService,
            TwitchEventValidationService
        >();

        // EventSub
        builder.Services.AddSingleton<EventSubService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<EventSubService>());

        // Сырые фреймы EventSub: публикуются в шину как EventSubRawEvent, откуда
        // MARS.Alerts (TwitchEventSubListener) рассылает их в оверлеи. Ветка
        // микросервисов потеряла регистрацию — без неё raid/online/offline/
        // moderator/vip/follow/redemption никогда не доходят до оверлеев.
        builder.Services.AddSingleton<EventSubRawForwarder>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<EventSubRawForwarder>());

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
        builder.Services.AddSingleton<IChannelRewardsService>(sp =>
            sp.GetRequiredService<ChannelRewardsService>()
        );
        builder.Services.AddSingleton<ChannelRewardsManager>();

        // WeddingAnniversary
        builder.Services.AddScoped<WeddingAnniversaryService>();

        // AudioController configuration
        builder.Services.Configure<AudioControllerOptions>(
            builder.Configuration.GetSection(AudioControllerOptions.SectionName)
        );

        // StreamBotNotifications
        builder.Services.AddHostedService<TwitchStreamStartupNotifications>();

        // YouTubeResolver
        builder.Services.AddSingleton<
            MARS.Platform.Infrastructure.YouTube.IYouTubeApi,
            MARS.Platform.Infrastructure.YouTube.YoutubeExplodeApi
        >();
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

        // Единственный издатель reward-событий (RewardRedeemedEvent) из настоящего
        // EventSub channel.channel_points_custom_reward_redemption.add. Потерян в
        // ветке микросервисов — без него ~33 обработчика наград в MARS.Alerts
        // получали пустой поток (блокер №4 аудита PR). Ранее ложный комментарий
        // утверждал, что он «работает через Wolverine-лисенер автоматически» —
        // это не так: это IHostedService, шина его не находит.
        builder.Services.AddSingleton<RewardRedemptionPublisher>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<RewardRedemptionPublisher>());
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

        // Форвардер стримов Tekken в Discord (отдельный hosted-service).
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
