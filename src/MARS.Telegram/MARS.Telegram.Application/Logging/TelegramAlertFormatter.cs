using System.Globalization;
using System.Text;
using MARS.Shared.Contracts.Telegram;

namespace MARS.Telegram.Application.Logging;

/// <summary>
/// Формирование текста алерта Prometheus/Alertmanager для чата.
/// </summary>
/// <remarks>
/// Сообщение читает человек в чате, поэтому здесь важны имя алерта, служба,
/// инстанс и аннотации. Разметка не используется: текст аннотаций содержит
/// произвольные символы, и наш разбор HTML превратил бы их в теги.
/// </remarks>
public static class TelegramAlertFormatter
{
    /// <summary>Лимит длины сообщения в Telegram.</summary>
    public const int MaxMessageLength = 4096;

    private const int MaxAlerts = 10;

    public static string Format(TelegramAlertWebhookRequest? request)
    {
        var result = new StringBuilder();

        var status = (request?.Status ?? string.Empty).Trim().ToLowerInvariant();
        var resolved = string.Equals(status, "resolved", StringComparison.Ordinal);

        result.Append(resolved ? "✅ Восстановлено" : "🚨 Алерт");
        result.Append(": ");
        result.Append(AlertName(request));
        result.Append('\n');

        var service = FirstLabel(request, "service");
        var instance = FirstLabel(request, "instance");

        if (!string.IsNullOrWhiteSpace(service))
        {
            result.Append("Служба: ");
            result.Append(service);
            result.Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(instance))
        {
            result.Append("Инстанс: ");
            result.Append(instance);
            result.Append('\n');
        }

        var alerts = request?.Alerts ?? [];
        var shown = Math.Min(alerts.Count, MaxAlerts);

        for (var i = 0; i < shown; i++)
        {
            var alert = alerts[i];
            var summary = Value(alert.Annotations, "summary");
            var description = Value(alert.Annotations, "description");

            if (i > 0)
            {
                result.Append("\n---\n");
            }

            if (!string.IsNullOrWhiteSpace(summary))
            {
                result.Append(summary);
                result.Append('\n');
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                result.Append(description);
                result.Append('\n');
            }

            var moment = alert.StartsAt ?? alert.EndsAt;

            if (moment is { } whenValue)
            {
                result.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "С {0:yyyy-MM-dd HH:mm:ss} UTC",
                        whenValue.ToUniversalTime()
                    )
                );
                result.Append('\n');
            }
        }

        if (alerts.Count > shown)
        {
            result.Append(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "(+{0} ещё)\n",
                    alerts.Count - shown
                )
            );
        }

        var text = result.ToString();

        if (text.Length > MaxMessageLength)
        {
            text = string.Concat(text.AsSpan(0, MaxMessageLength - 1), "…");
        }

        return text;
    }

    private static string AlertName(TelegramAlertWebhookRequest? request)
    {
        var result = Value(request?.CommonLabels ?? [], "alertname");

        if (string.IsNullOrWhiteSpace(result) && request?.Alerts.Count > 0)
        {
            result = Value(request.Alerts[0].Labels, "alertname");
        }

        return string.IsNullOrWhiteSpace(result) ? "Alert" : result;
    }

    private static string FirstLabel(TelegramAlertWebhookRequest? request, string key)
    {
        var result = Value(request?.CommonLabels ?? [], key);

        if (string.IsNullOrWhiteSpace(result) && request?.Alerts.Count > 0)
        {
            result = Value(request.Alerts[0].Labels, key);
        }

        // Групповые метки тоже могут нести service/instance.
        if (string.IsNullOrWhiteSpace(result))
        {
            result = Value(request?.GroupLabels ?? [], key);
        }

        return result ?? string.Empty;
    }

    private static string Value(IDictionary<string, string> source, string key)
    {
        var result = string.Empty;

        if (source.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            result = value;
        }

        return result;
    }
}