using MARS.Shared.Extensions;
using MARS.Shared.Grpc.Voice;
using MARS.TTS.Grpc;
using MARS.TTS.Services;

namespace MARS.TTS;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.TTS");

        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsEventBroadcaster<VoiceEvent>();
        builder.Services.AddControllers();

        // TTS services
        builder.Services.AddSingleton<ITtsMessageFilterService, TtsMessageFilterService>();
        builder.Services.AddSingleton<ITtsNotifier, TtsNotifier>();
        builder.Services.AddSingleton<ISevenTvEmoteService, SevenTvEmoteService>();
        builder.Services.AddHostedService(sp =>
            (SevenTvEmoteService)sp.GetRequiredService<ISevenTvEmoteService>()
        );

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapGrpcService<VoiceRecognitionGrpcService>();
        app.MapControllers();
        app.MapGet("/", () => "MARS.TTS is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
