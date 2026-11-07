namespace MARS.Shared.Kernel.Exceptions;

public class MarsException : Exception
{
    public MarsException(string message)
        : base(message) { }

    public MarsException(string message, Exception innerException)
        : base(message, innerException) { }
}
