using System.Net;

namespace MARS.Shared.Models;

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

public class OperationResult<T>
{
    public bool Success { get; set; }
    public T? Result { get; set; }

    /// <summary>
    /// Совместимое имя ошибки для вызывающего кода, который использует
    /// негeneric <see cref="OperationResult"/>. Новому коду следует читать
    /// <see cref="Message"/>.
    /// </summary>
    public string? ErrorMessage
    {
        get => Message;
        set => Message = value;
    }

    public string? Message { get; set; }
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
            Message = error,
            StatusCode = statusCode,
        };
}
