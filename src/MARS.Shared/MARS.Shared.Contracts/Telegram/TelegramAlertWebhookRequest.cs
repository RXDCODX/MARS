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
