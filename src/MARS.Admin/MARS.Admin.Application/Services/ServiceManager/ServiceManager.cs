using System.Reflection;
using MARS.Admin.Domain.Entities;
using MARS.Shared.Contracts.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Admin.Application.Services.ServiceManager;

public class ServiceManager : IServiceManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceManager> _logger;
    private readonly IServiceStateRepository _serviceStateRepository;
    private readonly ServiceEndpoints _endpoints;
    private readonly Dictionary<string, ServiceInfo> _serviceRegistry;

    public ServiceManager(
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceManager> logger,
        IServiceStateRepository serviceStateRepository,
        IOptions<ServiceEndpoints> endpoints
    )
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serviceStateRepository = serviceStateRepository;
        _endpoints = endpoints.Value;
        _serviceRegistry = BuildServiceRegistry();
    }

    /// <summary>
    /// Реестр строится из самих endpoint'ов: каждый сервис помечен
    /// <see cref="ManagedServiceAttribute"/> на своём свойстве. Раньше список
    /// дублировался кортежами и разошёлся — в нём не хватало Shikimori и Admin.
    /// </summary>
    private Dictionary<string, ServiceInfo> BuildServiceRegistry()
    {
        var result = new Dictionary<string, ServiceInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (
            var property in typeof(ServiceEndpoints).GetProperties(
                BindingFlags.Public | BindingFlags.Instance
            )
        )
        {
            var metadata = property.GetCustomAttribute<ManagedServiceAttribute>();
            if (metadata is null)
            {
                throw new InvalidOperationException(
                    $"У свойства {property.Name} нет атрибута {nameof(ManagedServiceAttribute)}."
                );
            }

            var endpoint = (string?)property.GetValue(_endpoints) ?? string.Empty;
            result[metadata.Name] = new ServiceInfo
            {
                Name = metadata.Name,
                DisplayName = metadata.DisplayName,
                Description = metadata.Description,
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

    public Task<IEnumerable<ServiceLog>> GetServiceLogsAsync(string serviceName, int count = 100)
    {
        List<ServiceLog> result = [];

        if (!string.IsNullOrWhiteSpace(serviceName) && count > 0)
        {
            for (var i = 0; i < Math.Min(count, 10); i++)
            {
                result.Add(
                    new ServiceLog
                    {
                        Timestamp = DateTime.Now.AddMinutes(-i),
                        Level =
                            i % 3 == 0 ? "Error"
                            : i % 2 == 0 ? "Warning"
                            : "Info",
                        Message = $"Log message {i} for service {serviceName}",
                        Exception = i % 5 == 0 ? $"Exception {i}" : null,
                    }
                );
            }
        }

        return Task.FromResult<IEnumerable<ServiceLog>>(result);
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
