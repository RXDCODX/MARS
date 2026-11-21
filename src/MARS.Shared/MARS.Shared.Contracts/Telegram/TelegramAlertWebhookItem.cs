namespace MARS.Shared.Contracts.Telegram;

/// <summary>Один алерт внутри webhook-вызова Alertmanager.</summary>
public class TelegramAlertWebhookItem
{
    /// <summary>Статус алерта (firing/resolved).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Метки алерта (alertname, service, instance, severity и т.д.).</summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>Аннотации алерта (summary, description).</summary>
    public Dictionary<string, string> Annotations { get; set; } = new();

    /// <summary>Время начала срабатывания (UTC).</summary>
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>Время разрешения (UTC), если алерт уже погас.</summary>
    public DateTimeOffset? EndsAt { get; set; }
}
