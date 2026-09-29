namespace MARS.Telegram.Entities;

public class WTelegramClientStatus
{
    public bool IsAuthenticated { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string? Phone { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsAwaitingCode { get; set; }
}
