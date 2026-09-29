using System.Diagnostics;
using MARS.SoundRequest.Configuration;
using Microsoft.Extensions.Options;

namespace MARS.SoundRequest.Services.SoundBarService;

public class SoundBarFactory(
    IHostEnvironment environment,
    IHttpClientFactory factory,
    ILogger<SoundBarFactory> logger,
    IOptions<HttpClientsConfiguration> httpClientsOptions
)
{
    private static readonly SoundBarServiceLocal SoundBarService = new();
    private static readonly SoundBarServiceLocal Instance = SoundBarService;
    private static SoundBarHttpClient? _instanceHttp;

    public ISoundBar CreateSoundBar()
    {
        var url = GetAudioControllerUrl();
        return (_instanceHttp ??= new SoundBarHttpClient(url, factory, logger));
    }

    private string GetAudioControllerUrl()
    {
        var config = httpClientsOptions.Value;
        var port = environment.IsProduction()
            ? config.AudioControllerProdPort
            : config.AudioControllerDevPort;
        if (port <= 0)
        {
            port = environment.IsProduction() ? 30695 : 30691;
        }

        return $"http://127.0.0.1:{port}";
    }

    private static bool IsRunningAsWindowsService()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.Version.Major < 5)
        {
            return !Environment.UserInteractive;
        }

        try
        {
            var process = Process.GetCurrentProcess();
            var modules = process.Modules.Cast<ProcessModule>();

            return modules.Any(m => m.ModuleName?.ToLower() is "services.exe" or "svchost.exe");
        }
        catch
        {
            return false;
        }
    }
}
