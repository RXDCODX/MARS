namespace MARS.Discord.Models;

public class OperationResult(bool success = false, string? message = null, object? data = null)
{
    public bool Success { get; set; } = success;
    public string? Message { get; set; } = message;
    public object? Data { get; set; } = data;

    public static OperationResult Ok(string? message = null, object? data = null) =>
        new(true, message, data);

    public static OperationResult Bad(string? message = null, object? data = null) =>
        new(false, message, data);

    public static bool operator !(OperationResult operationResult) => !operationResult.Success;

    public static bool operator true(OperationResult operationResult) =>
        operationResult.Success == true;

    public static bool operator false(OperationResult operationResult) =>
        operationResult.Success == false;
}

public class OperationResult<TData>(
    bool success = false,
    string? message = null,
    TData data = default!
) : OperationResult(success, message)
{
    public new TData Data { get; set; } = data;

    public static OperationResult<TData> Ok(string? message = null, TData data = default!) =>
        new(true, message, data);

    public static OperationResult<TData> Bad(string? message = null, TData data = default!) =>
        new(false, message, data);

    public static implicit operator TData(OperationResult<TData> operationResult) =>
        operationResult.Data;
}
