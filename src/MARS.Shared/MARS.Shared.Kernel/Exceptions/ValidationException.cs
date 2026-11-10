namespace MARS.Shared.Kernel.Exceptions;

public class ValidationException : MarsException
{
    public ValidationException(string message)
        : base(message) { }
}
