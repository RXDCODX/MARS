<<<<<<< HEAD:src/MARS.Discord/MARS.Discord.Host/Program.cs
using MARS.Discord.Application.Configuration;
using MARS.Discord.Application.Ports;
using MARS.Discord.Infrastructure.Services.CommandForwarder;
using MARS.Discord.Infrastructure.Services.Gateway;
using MARS.Discord.Infrastructure.Services.Media;
using MARS.Discord.Infrastructure.Services.PlayRequest;
using MARS.Discord.Infrastructure.Services.TtsVoiceRelay;
using MARS.Discord.Infrastructure.Services.YouTube;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using MARS.Platform.Infrastructure.YouTube;
=======
using MARS.Discord.Configuration;
using MARS.Discord.Services.CommandForwarder;
using MARS.Discord.Services.Gateway;
using MARS.Discord.Services.Media;
using MARS.Discord.Services.PlayRequest;
using MARS.Discord.Services.TtsVoiceRelay;
using MARS.Discord.Services.YouTube;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
>>>>>>> origin/main:src/MARS.Discord/Program.cs

namespace MARS.Discord.Host;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Discord");
        builder.AddMarsWolverine("MARS.Discord", typeof(Program).Assembly);

        builder.Services.Configure<DiscordConfiguration>(
            builder.Configuration.GetSection(DiscordConfiguration.Configuration)
        );

        builder.Services.AddSingleton<IDiscordGatewayService, DiscordGatewayService>();
        builder.Services.AddSingleton<IDiscordGatewayStatus, DiscordGatewayService>();
        builder.Services.AddSingleton<
            MARS.Platform.Infrastructure.YouTube.IYouTubeApi,
            MARS.Platform.Infrastructure.YouTube.YoutubeExplodeApi
        >();
        builder.Services.AddSingleton<IYouTubeResolver, YouTubeResolver>();
        builder.Services.AddSingleton<DiscordPlayAudioCacheService>();
        builder.Services.AddSingleton<IDiscordTtsVoiceRelayService, DiscordTtsVoiceRelayService>();

        // Сжатие вложений: ffmpeg запускается отдельным сервисом, чтобы
        // логика сжатия проверялась тестами без бинарников на машине.
        builder.Services.AddSingleton<IFfmpegRunner, FfmpegRunner>();
        builder.Services.AddSingleton<IMediaCompressor, MediaCompressor>();

        builder.Services.AddHostedService(sp =>
            (DiscordGatewayService)sp.GetRequiredService<IDiscordGatewayService>()
        );
        builder.Services.AddHostedService(sp =>
            (DiscordTtsVoiceRelayService)sp.GetRequiredService<IDiscordTtsVoiceRelayService>()
        );
        builder.Services.AddHostedService<DiscordPlayRequestService>();
        builder.Services.AddHostedService<DiscordCommandForwarderService>();

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();
        app.MapGet("/", () => "MARS.Discord is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
