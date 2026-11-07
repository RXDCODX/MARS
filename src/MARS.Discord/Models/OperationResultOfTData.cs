namespace MARS.Discord.Models;

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
