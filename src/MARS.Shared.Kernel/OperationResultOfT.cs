using System.Net;

namespace MARS.Shared.Kernel;

public class OperationResult<T>
{
    public bool Success { get; set; }
    public T? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    public static OperationResult<T> Ok(T result) =>
        new()
        {
            Success = true,
            Result = result,
            StatusCode = HttpStatusCode.OK,
        };

    public static OperationResult<T> Fail(
        string error,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest
    ) =>
        new()
        {
            Success = false,
            ErrorMessage = error,
            StatusCode = statusCode,
        };
}
