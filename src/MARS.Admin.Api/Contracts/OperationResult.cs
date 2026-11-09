namespace MARS.Admin.Api.Contracts;

public class OperationResult(bool success = false, string? message = null, object? data = null)
{
    public bool Success { get; set; } = success;
    public string? Message { get; set; } = message;
    public object? Data { get; set; } = data;

    public static OperationResult Ok(string? message = null, object? data = null) =>
        new(true, message, data);

    public static OperationResult Bad(string? message = null, object? data = null) =>
        new(false, message, data);

    public static OperationResult Some(
        bool success = false,
        string? message = null,
        object? data = null
    ) => new(success, message, data);

    public static bool operator !(OperationResult operationResult) => !operationResult.Success;

    public static bool operator true(OperationResult operationResult) =>
        operationResult.Success == true;

    public static bool operator false(OperationResult operationResult) =>
        operationResult.Success == false;

    // Completes the conditional logical operators: "&&" first checks operator
    // false, then combines the right-hand result only when the left succeeded.
    // Preserve the first failure so its diagnostic message/data are not lost.
    public static OperationResult operator &(OperationResult left, OperationResult right) =>
        left.Success ? right : left;
}
