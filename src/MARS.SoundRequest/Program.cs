using MARS.Shared.Extensions;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Grpc;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.SoundBarService;
using MARS.SoundRequest.Services.SoundCloud;
using MARS.SoundRequest.Services.Spotify;
using MARS.SoundRequest.Services.YouTube;

namespace MARS.SoundRequest;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.SoundRequest", "MediaDb");

        // Configuration
        builder.Services.Configure<SoundRequestConfiguration>(
            builder.Configuration.GetSection(SoundRequestConfiguration.SectionName)
        );
        builder.Services.Configure<SpotifySoundRequestConfiguration>(
            builder.Configuration.GetSection(SpotifySoundRequestConfiguration.SectionName)
        );
        builder.Services.Configure<HttpClientsConfiguration>(
            builder.Configuration.GetSection(HttpClientsConfiguration.Configuration)
        );

        // Database
        builder.Services.AddMarsDbContext<MediaDbContext>(
            builder.Configuration,
            "media",
            "MediaDb"
        );

        // gRPC: подписки на состояние плеера и вызовы от плеера на странице
        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsEventBroadcaster<SoundRequestEvent>();

        // HttpClient-ы. Аудит: SpotifyAuthService создавал new HttpClient() на каждый
        // вызов. Именованный клиент переиспользует сокеты.
        builder.Services.AddHttpClient("spotify-auth");
        builder.Services.AddHttpClient("youtube-oembed");

        // Services
        builder.Services.AddSingleton<StateManager>();
        builder.Services.AddSingleton<SoundRequestUserQueue>();
        builder.Services.AddSingleton<TrackEventRelay>();
        builder.Services.AddSingleton<SoundRequestNotifier>();
        builder.Services.AddSingleton<SpotifyAuthService>();
        // Аудит: двойная регистрация AddHttpClient<SpotifyApiClient>() + AddSingleton
        // создавала два разных экземпляра. Typed-клиент transient и теряет кэш
        // device id, а AddSingleton перекрывал его и конструировался напрямую.
        // Оставляем единственную transient-регистрацию через фабрику, а потребителям
        // отдаём один общий экземпляр через обёртку ниже.
        builder.Services.AddHttpClient<SpotifyApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        builder.Services.AddSingleton<SpotifyPlaybackService>();
        builder.Services.AddSingleton<YouTubeResolver>();
        builder.Services.AddSingleton<SpotifyResolver>();
        builder.Services.AddSingleton<SoundCloudResolver>();
        builder.Services.AddSingleton<MainPlayer>();
        builder.Services.AddSingleton<SoundRequestCommandsService>();

        // SoundBar services
        builder.Services.AddSingleton<SoundBarFactory>();
        builder.Services.AddSingleton<SoundMuteCoordinator>();

        // Hosted services
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MainPlayer>());

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();

        app.MapGrpcService<SoundRequestGrpcService>();
        app.MapControllers();
        app.MapGet("/", () => "MARS.SoundRequest is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
