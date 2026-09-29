using MARS.Discord.Configuration;
using MARS.Discord.Services.Gateway;
using MARS.Discord.Services.PlayRequest;
using MARS.Discord.Services.TtsVoiceRelay;
using MARS.Discord.Services.YouTube;
using MARS.Shared.Extensions;

namespace MARS.Discord;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Discord");

        builder.Services.Configure<DiscordConfiguration>(
            builder.Configuration.GetSection(DiscordConfiguration.Configuration)
        );

        builder.Services.AddSingleton<IDiscordGatewayService, DiscordGatewayService>();
        builder.Services.AddSingleton<YouTubeResolver>();
        builder.Services.AddSingleton<DiscordPlayAudioCacheService>();
        builder.Services.AddSingleton<IDiscordTtsVoiceRelayService, DiscordTtsVoiceRelayService>();

        builder.Services.AddHostedService(sp =>
            (DiscordGatewayService)sp.GetRequiredService<IDiscordGatewayService>()
        );
        builder.Services.AddHostedService(sp =>
            (DiscordTtsVoiceRelayService)sp.GetRequiredService<IDiscordTtsVoiceRelayService>()
        );
        builder.Services.AddHostedService<DiscordPlayRequestService>();

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
