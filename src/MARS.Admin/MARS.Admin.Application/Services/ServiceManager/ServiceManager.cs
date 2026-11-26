using MARS.Admin.Application.Configuration;
using MARS.Admin.Application.Services.Logging;
using MARS.Admin.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Admin.Application.Services.ServiceManager;

public class ServiceManager : IServiceManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceManager> _logger;
    private readonly IServiceStateRepository _serviceStateRepository;
    private readonly ILogQueryService _logQueryService;
    private readonly ServiceEndpoints _endpoints;
    private readonly Dictionary<string, ServiceInfo> _serviceRegistry;

    public ServiceManager(
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceManager> logger,
        IServiceStateRepository serviceStateRepository,
        ILogQueryService logQueryService,
        IOptions<ServiceEndpoints> endpoints
    )
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serviceStateRepository = serviceStateRepository;
        _logQueryService = logQueryService;
        _endpoints = endpoints.Value;
        _serviceRegistry = BuildServiceRegistry();
    }

    private Dictionary<string, ServiceInfo> BuildServiceRegistry()
    {
        var result = new Dictionary<string, ServiceInfo>(StringComparer.OrdinalIgnoreCase);

        var services = new (string Name, string DisplayName, string Description, string Endpoint)[]
        {
            ("twitch-core", "Twitch Core", "Основной Twitch сервис", _endpoints.TwitchCore),
            ("waifu-gacha", "Waifu Gacha", "Сервис вайфу гача", _endpoints.WaifuGacha),
            ("telegram", "Telegram", "Telegram бот", _endpoints.Telegram),
            ("discord", "Discord", "Discord бот", _endpoints.Discord),
            ("commands", "Commands", "Сервис команд", _endpoints.Commands),
            ("sound-request", "Sound Request", "Сервис звуковых запросов", _endpoints.SoundRequest),
            ("tts", "TTS", "Сервис синтеза речи", _endpoints.TTS),
            ("obs", "OBS", "OBS интеграция", _endpoints.OBS),
            ("alerts", "Alerts", "Сервис алертов", _endpoints.Alerts),
            ("scoreboard", "Scoreboard", "Сервис табло", _endpoints.Scoreboard),
            ("cinema-queue", "Cinema Queue", "Сервис очереди кинотеатра", _endpoints.CinemaQueue),
            ("media-storage", "Media Storage", "Сервис хранения медиа", _endpoints.MediaStorage),
        };

        foreach (var (name, displayName, description, endpoint) in services)
        {
            result[name] = new ServiceInfo
            {
                Name = name,
                DisplayName = displayName,
                Description = description,
                Status = ServiceStatus.Unknown,
                IsEnabled = true,
                HealthEndpoint = $"{endpoint}/health",
            };
        }

        return result;
    }

    public async Task<Dictionary<string, ServiceStatus>> GetServicesStatusAsync()
    {
        var result = new Dictionary<string, ServiceStatus>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in _serviceRegistry)
        {
            result[service.Key] = await CheckServiceHealthAsync(service.Value);
        }

        return result;
    }

    public Task<bool> StartServiceAsync(string serviceName)
    {
        _logger.LogWarning(
            "Start service requested for {ServiceName} — not supported in admin microservice",
            serviceName
        );
        return Task.FromResult(false);
    }

    public Task<bool> StopServiceAsync(string serviceName)
    {
        _logger.LogWarning(
            "Stop service requested for {ServiceName} — not supported in admin microservice",
            serviceName
        );
        return Task.FromResult(false);
    }

    public async Task<bool> RestartServiceAsync(string serviceName)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            var stopped = await StopServiceAsync(serviceName);
            if (stopped)
            {
                await Task.Delay(2000);
                result = await StartServiceAsync(serviceName);
            }
        }

        return result;
    }

    public async Task<ServiceInfo?> GetServiceInfoAsync(string serviceName)
    {
        ServiceInfo? result = null;

        if (
            !string.IsNullOrWhiteSpace(serviceName)
            && _serviceRegistry.TryGetValue(serviceName, out var serviceInfo)
        )
        {
            serviceInfo.Status = await CheckServiceHealthAsync(serviceInfo);
            result = serviceInfo;
        }

        return result;
    }

    public async Task<IEnumerable<ServiceLog>> GetServiceLogsAsync(
        string serviceName,
        int count = 100,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(serviceName) || count <= 0)
        {
            return [];
        }

        var logs = await _logQueryService.QueryAsync(
            new ServiceLogQuery
            {
                Service = serviceName,
                Limit = count,
                Hours = 24,
            },
            cancellationToken
        );

        return logs;
    }

    public async Task<bool> SetServiceActiveAsync(string serviceName, bool isActive)
    {
        var result = false;

        if (
            !string.IsNullOrWhiteSpace(serviceName)
            && _serviceRegistry.TryGetValue(serviceName, out var serviceInfo)
        )
        {
            serviceInfo.IsEnabled = isActive;
            _logger.LogInformation(
                "Service {ServiceName} active state set to {IsActive}",
                serviceName,
                isActive
            );
            result = true;

            try
            {
                await _serviceStateRepository.SetServiceActiveAsync(serviceName, isActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to persist active state for {ServiceName}",
                    serviceName
                );
            }
        }

        return result;
    }

    public async Task<IEnumerable<ServiceInfo>> GetAllServicesAsync()
    {
        var result = new List<ServiceInfo>();

        foreach (var service in _serviceRegistry.Values)
        {
            service.Status = await CheckServiceHealthAsync(service);
            result.Add(service);
        }

        return result;
    }

    private async Task<ServiceStatus> CheckServiceHealthAsync(ServiceInfo serviceInfo)
    {
        var result = ServiceStatus.Unknown;

        if (string.IsNullOrWhiteSpace(serviceInfo.HealthEndpoint))
        {
            return result;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("HealthCheck");
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(serviceInfo.HealthEndpoint);

            result = response.IsSuccessStatusCode ? ServiceStatus.Running : ServiceStatus.Error;

            serviceInfo.LastActivity = DateTime.Now;

            if (result == ServiceStatus.Running && serviceInfo.StartTime is null)
            {
                serviceInfo.StartTime = DateTime.Now;
            }
        }
        catch (TaskCanceledException)
        {
            result = ServiceStatus.Error;
        }
        catch (HttpRequestException)
        {
            result = ServiceStatus.Stopped;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed for {ServiceName}", serviceInfo.Name);
            result = ServiceStatus.Error;
        }

        return result;
    }
}
