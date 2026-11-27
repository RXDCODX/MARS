using MARS.Discord.Application.Configuration;
using MARS.Discord.Application.Ports;
using MARS.Discord.Infrastructure.Services.CommandForwarder;
using MARS.Discord.Infrastructure.Services.Gateway;
using MARS.Discord.Infrastructure.Services.Media;
using MARS.Discord.Infrastructure.Services.PlayRequest;
using MARS.Discord.Infrastructure.Services.TtsVoiceRelay;
using MARS.Discord.Infrastructure.Services.YouTube;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Platform.Infrastructure.YouTube;

namespace MARS.Discord.Host;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Discord");
        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsGrpcClient();
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

        app.MapGrpcService<MARS.Discord.Api.Grpc.DiscordMessageGrpcService>()
            .RequireAuthorization(ServiceAuthExtensions.PolicyName);
        app.MapGet("/", () => "MARS.Discord is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        await app.RunMarsSchemaMigrationsAsync();

        await app.RunAsync();
    }
}
