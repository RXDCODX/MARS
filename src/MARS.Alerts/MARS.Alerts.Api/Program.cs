using MARS.Alerts.Application;
using MARS.Alerts.Application.Alerts;

using MARS.Alerts.Application.TwitchMessages.TriggerWords;
using MARS.Alerts.Application.TwitchRewards.RewardInput;
using MARS.Alerts.Application.TwitchMessages;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Listeners;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Infrastructure.Alerts;
using MARS.Alerts.Infrastructure.Audio;
using MARS.Alerts.Infrastructure.Overlay;
using MARS.Alerts.Infrastructure.SevenTv;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Modules.Alerts.Application.Adhd;
using MARS.Modules.Alerts.Infrastructure.Postgres.Data;
using MARS.Modules.Alerts.Infrastructure.Postgres.Services;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.Http.Matoi;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts;
using MARS.WaifuGacha.Infrastructure.Http;
using MARS.WaifuGacha.Models.Rest;
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

        // Отдельный OpenAPI-документ для SignalR-хаба оверлея.
        // Класс переименован в AlertsHub, но внешний маршрут остаётся /hubs/overlay.
        builder.AddMarsHubsSwagger("MARS.Alerts", typeof(AlertsHub).Assembly);
        builder.AddMarsWolverine(
            "MARS.Alerts",
            typeof(Program).Assembly,
            typeof(TwitchMessagesListener).Assembly,
            typeof(RewardAlertListener).Assembly,
            typeof(RewardInputMessageListener).Assembly
        );

        builder.Services.AddMarsDbContext<AlertsDbContext>(
            builder.Configuration,
            "alerts",
            "AlertsDb"
        );

        builder.Services.AddMarsSignalR();

        // MARS.Alerts — владелец оверлея: держит широковещатель в памяти, отдаёт
        // состояния не имеет, поэтому зарегистрировано как hosted service: без
        // него хаб принимал бы подключения и не получал бы ни одного события.
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();

        // Координатор одновременного глушения звука: микшер приложений
        // (AudioController на хосте), TTS и плеер SoundRequest.
        builder.Services.AddSingleton<SoundMuteCoordinator>();
        builder.Services.AddSingleton<ISoundMuteCoordinator>(sp =>
            sp.GetRequiredService<SoundMuteCoordinator>()
        );
        builder.Services.AddMarsAudioControllerClient();
        builder.Services.AddMarsSoundControlClients();

        // Триггерные алерты: список алертов читает владелец — MARS.MediaStorage.
        builder.Services.AddMarsServiceClients(builder.Configuration);
        builder.Services.AddMarsServiceClient<IMediaStorageClient, MediaStorageClient>(
            ServiceClientExtensions.MediaStorageHttpClientName
        );
        builder.Services.AddMarsServiceClient<IWaifuGachaClient, WaifuGachaClient>(
            ServiceClientExtensions.WaifuGachaHttpClientName
        );
        builder.Services.AddSingleton<IEnabledAlertSource, MediaStorageEnabledAlertSource>();

        // Раскладка ADHD-экрана. Настройка принадлежит этому сервису: хаб
        // оверлея сохраняет её и отдаёт подключившимся, поэтому читать и менять
        // её может только MARS.Alerts, где живёт база. Сервис stateless
        // (контекст создаётся фабрикой на вызов), а хаб живёт всё время
        // подключения — регистрация синглтоном.
        builder.Services.AddSingleton<IAdhdLayoutService, AdhdLayoutService>();

        // Поиск постов booru для награды RANDOM ART. Клиент общий с
        // MARS.Telegram, регистрируется в MARS.Shared и сам берёт адрес и ключ
        // из секции Matoi.
        builder.Services.AddMatoiClient(builder.Configuration);

        // Shared Twitch reward dependencies
        builder.Services.AddSingleton<
            ITwitchEventValidationService,
            TwitchEventValidationService
        >();
        builder.Services.AddSingleton<RickRollerService>();
        builder.Services.AddSingleton<HighlitedMessage>();
        // MikuMondayTracksService удалён: логика теперь в MARS.WaifuGacha,
        // клиент обращается через IWaifuGachaClient.

        // Все награды наследуются от RewardHandlerBase. Менеджер получает события
        // погашения, обычного чата, reward-input и EventSub; общие Twitch-события
        // рассылаются подписчикам RewardHandlerBase. Каждый обработчик регистрируется один раз.
        builder.Services.AddMarsRewardHandlers();
        builder.Services.AddSingleton<RewardEventManager>(_ => new RewardEventManager([]));

        // Потребители RabbitMQ теперь являются Wolverine-лисенерами и
        // обнаруживаются автоматически через AddMarsWolverine. Отдельная
        // регистрация AddHostedService не требуется.
        builder.Services.AddSingleton<TriggerWordAlertDispatcher>();
        builder.Services.AddSingleton<RewardBoundAlertDispatcher>();
        builder.Services.AddSingleton<AllRefundState>();

        // Meme workers
        builder.Services.AddHostedService<RandomMemeWorker>();

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

        // Хаб оверлея. Путь совпадает с тем, что проксирует Gateway, и должен
        // оставаться врознь с gRPC-стримом на 8081: тот живёт по HTTP/2, а хаб
        // обслуживает браузер через :8080.
        app.MapHub<AlertsHub>("/hubs/overlay");
        // Хаб информации о треке. Отдельный путь и отдельное реле: событие
        // трека едет из своего широковещателя и подписка на него своя.
        app.MapControllers();
        app.MapGet("/", () => "MARS.Alerts is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
