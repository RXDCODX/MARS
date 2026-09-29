namespace MARS.Shared.Exceptions;

public class MarsException : Exception
{
    public MarsException(string message)
        : base(message) { }

    public MarsException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class ServiceUnavailableException : MarsException
{
    public string ServiceName { get; }

    public ServiceUnavailableException(string serviceName)
        : base($"Service '{serviceName}' is unavailable")
    {
        ServiceName = serviceName;
    }
}

public class ValidationException : MarsException
{
    public ValidationException(string message)
        : base(message) { }
}
