using MARS.AudioController.Configuration;
using MARS.AudioController.Services;
using MARS.AudioController.Services.Naaudio;
using MARS.Shared.Extensions;

namespace MARS.AudioController;

public class Program
{
    private static void Main(string[] args)
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

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
