using MARS.Alerts.Configuration;
using MARS.Alerts.Data;
using MARS.Alerts.Models;
using MARS.Alerts.Services;
using MARS.Alerts.Services.Adhd;
using MARS.Alerts.Services.PyroAlerts;
using MARS.Alerts.Services.Synthesizer;
using MARS.Alerts.Services.Twitch.Rewards;
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
        builder.Services.AddMarsEventBroadcaster<TelegramusEvent>();
        builder.Services.AddMarsEventBroadcaster<TunaEvent>();
        builder.Services.AddSingleton<ITelegramusNotifier, TelegramusNotifier>();
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();

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

        // Reward handlers. Каждый реализует IRewardAlertHandler и разбирается
        // единственным RewardAlertConsumer, поэтому больше не поднимается
        // отдельная BackgroundService с собственным AMQP-соединением на награду.
        AddRewardHandler<AdhdSuperpowerHandler>();
        AddRewardHandler<AgaHandler>();
        AddRewardHandler<AllRefundHandler>();
        AddRewardHandler<BadToBoneHandler>();
        AddRewardHandler<ByeHandler>();
        AddRewardHandler<CinemaRequestHandler>();
        AddRewardHandler<CloseGameHandler>();
        AddRewardHandler<ConfettiHandler>();
        AddRewardHandler<CreditsHandler>();
        AddRewardHandler<CringeHandler>();
        AddRewardHandler<DanceDanceHandler>();
        AddRewardHandler<Edge0100AlertHandler>();
        AddRewardHandler<FireworksHandler>();
        AddRewardHandler<FumoFridayNightHandler>();
        AddRewardHandler<GaoAlertHandler>();
        AddRewardHandler<HelloHandler>();
        AddRewardHandler<IntelligenceHandler>();
        AddRewardHandler<LegBumHandler>();
        AddRewardHandler<MichaelTimeHandler>();
        AddRewardHandler<MikuScreamerHandler>();
        AddRewardHandler<PedroHandler>();
        AddRewardHandler<PhonkEditHandler>();
        AddRewardHandler<RandomArtHandler>();
        AddRewardHandler<SelectGameHandler>();
        AddRewardHandler<SkibidibopHandler>();
        AddRewardHandler<SkibidibopLongHandler>();
        AddRewardHandler<StatusQuestionHandler>();
        AddRewardHandler<StoneHandler>();
        AddRewardHandler<TikTokEditHandler>();
        AddRewardHandler<TyazheloHandler>();
        AddRewardHandler<WednsdayFrogHandler>();
        AddRewardHandler<WhatHandler>();

        // MIKU MIKU BEAM дополнительно считает состав участников чата,
        // поэтому один и тот же экземпляр отдаётся обоим интерфейсам.
        builder.Services.AddSingleton<MikuMikuBeamHandler>();
        builder.Services.AddSingleton<IRewardAlertHandler>(sp =>
            sp.GetRequiredService<MikuMikuBeamHandler>()
        );
        builder.Services.AddSingleton<IChatUserTrackingHandler>(sp =>
            sp.GetRequiredService<MikuMikuBeamHandler>()
        );

        // Потребители RabbitMQ: три соединения на весь сервис вместо ~30.
        builder.Services.AddHostedService<RewardAlertConsumer>();
        builder.Services.AddHostedService<SystemEventsConsumer>();
        builder.Services.AddHostedService<ChatUserConsumer>();
        builder.Services.AddHostedService<TwitchMediaAlerts>();

        // Meme workers
        builder.Services.AddHostedService<RandomMemeWorker>();
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
        app.MapControllers();
        app.MapGet("/", () => "MARS.Alerts is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();

        void AddRewardHandler<THandler>()
            where THandler : class, IRewardAlertHandler
        {
            builder.Services.AddSingleton<IRewardAlertHandler, THandler>();
        }
    }
}
