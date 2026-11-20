using MARS.Shared.Contracts.Telegram;
using MARS.Telegram.Application.Logging;

namespace MARS.Telegram.Tests.Services;

public class TelegramAlertFormatterTests
{
    [Fact]
    public void Пустой_Запрос_Не_Падает()
    {
        var text = TelegramAlertFormatter.Format(null);
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void Firing_Содержит_Имя_Алерта_И_Аннотации()
    {
        var request = new TelegramAlertWebhookRequest
        {
            Status = "firing",
            CommonLabels = new Dictionary<string, string>
            {
                ["alertname"] = "MarsLogError",
                ["service"] = "twitch-core",
                ["instance"] = "twitch-core",
            },
            Alerts =
            [
                new TelegramAlertWebhookItem
                {
                    Status = "firing",
                    Labels = new Dictionary<string, string>
                    {
                        ["alertname"] = "MarsLogError",
                        ["service"] = "twitch-core",
                        ["instance"] = "twitch-core",
                    },
                    Annotations = new Dictionary<string, string>
                    {
                        ["summary"] = "Ошибки в журнале",
                        ["description"] = "За последние 5 минут: 3.",
                    },
                },
            ],
        };

        var text = TelegramAlertFormatter.Format(request);

        Assert.Contains("🚨", text);
        Assert.Contains("MarsLogError", text);
        Assert.Contains("twitch-core", text);
        Assert.Contains("Ошибки в журнале", text);
        Assert.Contains("За последние 5 минут", text);
    }

    [Fact]
    public void Resolved_Содержит_Галочку()
    {
        var request = new TelegramAlertWebhookRequest
        {
            Status = "resolved",
            Alerts = [new TelegramAlertWebhookItem { Status = "resolved" }],
        };

        var text = TelegramAlertFormatter.Format(request);

        Assert.Contains("✅", text);
    }

    [Fact]
    public void Длинный_Текст_Обрезается_До_Лимита()
    {
        var request = new TelegramAlertWebhookRequest
        {
            Status = "firing",
            Alerts =
            [
                new TelegramAlertWebhookItem
                {
                    Status = "firing",
                    Annotations = new Dictionary<string, string>
                    {
                        ["description"] = new string('x', 5000),
                    },
                },
            ],
        };

        var text = TelegramAlertFormatter.Format(request);

        Assert.True(text.Length <= TelegramAlertFormatter.MaxMessageLength);
    }
}