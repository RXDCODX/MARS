using MARS.Admin.Entities;

namespace MARS.Admin.Services.ServiceManager;

public interface IServiceManager
{
    Task<Dictionary<string, ServiceStatus>> GetServicesStatusAsync();
    Task<bool> StartServiceAsync(string serviceName);
    Task<bool> StopServiceAsync(string serviceName);
    Task<bool> RestartServiceAsync(string serviceName);
    Task<ServiceInfo?> GetServiceInfoAsync(string serviceName);
    Task<IEnumerable<ServiceLog>> GetServiceLogsAsync(string serviceName, int count = 100);
    Task<bool> SetServiceActiveAsync(string serviceName, bool isActive);
    Task<IEnumerable<ServiceInfo>> GetAllServicesAsync();
}
