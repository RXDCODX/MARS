using System.Net;

namespace MARS.Shared.Kernel;

public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    public static OperationResult Ok() => new() { Success = true, StatusCode = HttpStatusCode.OK };

    public static OperationResult Fail(
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
