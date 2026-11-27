using MARS.AudioController.Application.Configuration;
using MARS.AudioController.Application.Ports;
using MARS.AudioController.Infrastructure.Services.Naaudio;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Postgres.Extensions;

namespace MARS.AudioController.Host;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.AudioController");

        builder.Services.Configure<AudioControllerConfiguration>(
            builder.Configuration.GetSection(AudioControllerConfiguration.SectionName)
        );

        builder.Services.AddSingleton<IAudioDeviceService, NaaudioAudioDeviceService>();
        builder.Services.AddSingleton<IAudioMixerService, NaaudioAudioMixerService>();

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();
        app.MapGet("/", () => "MARS.AudioController is running");

        await app.RunMarsSchemaMigrationsAsync();

        await app.RunAsync();
    }
}
