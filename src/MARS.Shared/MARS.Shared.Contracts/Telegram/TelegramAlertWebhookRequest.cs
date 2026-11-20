namespace MARS.Shared.Contracts.Telegram;

/// <summary>
/// Тело webhook-вызова Alertmanager в MARS.Telegram.
/// </summary>
/// <remarks>
/// Контракт повторяет форму, в которой Alertmanager шлёт webhook-уведомления
/// (версия «4»): службе MARS.Telegram не нужно знать больше ничего, кроме
/// полей, которые она показывает в чате. Лишние поля отбрасываются, отсутствующие
/// заменяются пустыми значениями — так Alertmanager может прислать как один
/// алерт, так и целую группу.
/// </remarks>
public class TelegramAlertWebhookRequest
{
    /// <summary>Статус всей группы алертов (firing/resolved).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Имя получателя в Alertmanager (для журнала).</summary>
    public string Receiver { get; set; } = string.Empty;

    /// <summary>Общие метки группы.</summary>
    public Dictionary<string, string> GroupLabels { get; set; } = new();

    /// <summary>Общие метки всех алертов в группе.</summary>
    public Dictionary<string, string> CommonLabels { get; set; } = new();

    /// <summary>Алерты в группе.</summary>
    public List<TelegramAlertWebhookItem> Alerts { get; set; } = [];
}

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

/// <summary>Результат доставки алерта в Telegram.</summary>
public class TelegramAlertResult
{
    /// <summary>Сколько чатов получили сообщение.</summary>
    public int Recipients { get; set; }
}
