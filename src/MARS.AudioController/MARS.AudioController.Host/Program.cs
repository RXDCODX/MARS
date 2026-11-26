using MARS.AudioController.Application.Configuration;
using MARS.AudioController.Application.Ports;
using MARS.AudioController.Infrastructure.Services.Naaudio;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;

namespace MARS.AudioController.Host;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.AudioController");
        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsGrpcClient();

        builder.Services.Configure<AudioControllerConfiguration>(
            builder.Configuration.GetSection(AudioControllerConfiguration.SectionName)
        );

        builder.Services.AddSingleton<IAudioDeviceService, NaaudioAudioDeviceService>();
        builder.Services.AddSingleton<IAudioMixerService, NaaudioAudioMixerService>();

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();

        app.MapGrpcService<MARS.AudioController.Api.Rest.Grpc.AudioControllerGrpcService>()
            .RequireAuthorization(ServiceAuthExtensions.PolicyName);
        app.MapGet("/", () => "MARS.AudioController is running");

        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
