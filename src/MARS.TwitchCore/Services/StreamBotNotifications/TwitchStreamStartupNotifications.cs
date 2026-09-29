using System.Net.Http;
using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Stream;
using TwitchLib.EventSub.Websockets;

namespace MARS.TwitchCore.Services.StreamBotNotifications;

public class TwitchStreamStartupNotifications : IHostedService
{
    private readonly ILogger<TwitchStreamStartupNotifications> _logger;
    private readonly ITwitchClient _twitchClient;
    private readonly EventSubWebsocketClient _wsClient;
    private readonly IOptions<AudioControllerOptions> _audioControllerOptions;
    private readonly IHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;

    public TwitchStreamStartupNotifications(
        ILogger<TwitchStreamStartupNotifications> logger,
        ITwitchClient twitchClient,
        IHostApplicationLifetime lifetime,
        EventSubWebsocketClient wsClient,
        IOptions<AudioControllerOptions> audioControllerOptions,
        IHostEnvironment environment,
        IHttpClientFactory httpClientFactory
    )
    {
        _logger = logger;
        _twitchClient = twitchClient;
        _wsClient = wsClient;
        _audioControllerOptions = audioControllerOptions;
        _environment = environment;
        _httpClientFactory = httpClientFactory;

        lifetime.ApplicationStarted.Register(() =>
        {
            _wsClient.StreamOffline += PubSibOfflineStream;
            _wsClient.StreamOnline += PubSubOnlineOnStreamUp;
        });
    }

    internal Task PubSubOnlineOnStreamUp(object? sender, StreamOnlineArgs streamOnlineArgs)
    {
        return HandleStreamOnlineAsync();
    }

    internal Task PubSibOfflineStream(object? sender, StreamOfflineArgs args)
    {
        return _twitchClient.SendMessageToMainTwitchAsync(
            "Та куда стрим вырубил Stressed",
            _logger
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task HandleStreamOnlineAsync()
    {
        var audioControllerAvailable = await IsAudioControllerAvailableAsync();

        if (!audioControllerAvailable)
        {
            var reminderMessage =
                "Аудиоконтроллер не запущен. Проверь его запуск, чтобы звуковые запросы работали корректно.";
            await _twitchClient.SendMessageToMainTwitchAsync(reminderMessage, _logger);
        }
    }

    private async Task<bool> IsAudioControllerAvailableAsync()
    {
        try
        {
            var config = _audioControllerOptions.Value;
            var port = _environment.IsProduction()
                ? config.AudioControllerProdPort
                : config.AudioControllerDevPort;
            if (port <= 0)
            {
                port = _environment.IsProduction() ? 30695 : 30691;
            }

            var healthUrl = $"http://127.0.0.1:{port}/api/health";
            // Аудит: new HttpClient() на каждый health-check. Фабрика переиспользует сокет,
            // таймаут задаётся на конкретном клиенте, а не на общем.
            using var httpClient = _httpClientFactory.CreateClient("audio-controller-health");
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var response = await httpClient.GetAsync(healthUrl, timeoutCts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio controller health-check failed");
            return false;
        }
    }
}
