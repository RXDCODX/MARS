namespace MARS.Shared.Contracts.Telegram;

/// <summary>Результат доставки алерта в Telegram.</summary>
public class TelegramAlertResult
{
    /// <summary>Сколько чатов получили сообщение.</summary>
    public int Recipients { get; set; }
}
