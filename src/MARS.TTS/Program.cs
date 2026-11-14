using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using MARS.Shared.Contracts.Voice;
using MARS.TTS.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.TTS;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.TTS");

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
        app.MapControllers();
        app.MapGet("/", () => "MARS.TTS is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
