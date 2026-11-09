namespace MARS.Admin.Application.Services.ServiceManager;

/// <summary>Persists the operational state of an Admin-managed service.</summary>
public interface IServiceStateRepository
{
    Task SetServiceActiveAsync(string serviceName, bool isActive);
}
