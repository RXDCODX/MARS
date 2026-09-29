namespace MARS.Telegram.Entities;

public class WTelegramOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public WTelegramClientStatus? ClientStatus { get; set; }

    public static WTelegramOperationResult CreateSuccess(
        string message,
        WTelegramClientStatus? clientStatus = null
    ) =>
        new()
        {
            Success = true,
            Message = message,
            ClientStatus = clientStatus,
        };

    public static WTelegramOperationResult CreateFailure(
        string message,
        string? errorDetails = null
    ) =>
        new()
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails,
        };
}
