using MARS.Shared.Extensions;
using MARS.TTS.Hubs;
using MARS.TTS.Services;

namespace MARS.TTS;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.TTS");

        builder.Services.AddSignalR();
        builder.Services.AddControllers();

        // TTS services
        builder.Services.AddSingleton<ITtsMessageFilterService, TtsMessageFilterService>();
        builder.Services.AddSingleton<ITtsHubBroadcaster, TtsHubBroadcaster>();
        builder.Services.AddSingleton<ISevenTvEmoteService, SevenTvEmoteService>();
        builder.Services.AddHostedService(sp =>
            (SevenTvEmoteService)sp.GetRequiredService<ISevenTvEmoteService>()
        );

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapHub<VoiceRecognitionHub>("/hubs/tts");
        app.MapControllers();
        app.MapGet("/", () => "MARS.TTS is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
