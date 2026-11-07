namespace MARS.Shared.Kernel.Exceptions;

public class ServiceUnavailableException : MarsException
{
    public string ServiceName { get; }

    public ServiceUnavailableException(string serviceName)
        : base($"Service '{serviceName}' is unavailable")
    {
        ServiceName = serviceName;
    }
}
