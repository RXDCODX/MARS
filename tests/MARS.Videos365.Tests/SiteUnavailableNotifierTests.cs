using MARS.Shared.Configuration;
using MARS.Shared.Telegram;
using MARS.Videos365.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Videos365.Tests;

/// <summary>
/// Уведомление администраторов о недоступности сайта-источника.
/// Сеть не используется: мессенджер подменён, поведение считается.
/// </summary>
public class SiteUnavailableNotifierTests
{
    private const long FirstAdmin = 1001;
    private const long SecondAdmin = 1002;

    private static readonly Uri Site = new("https://example.test");

    private readonly List<(long ChatId, string Text)> _sent = [];

    private sealed class RecordingMessenger : ITelegramAdminMessenger
    {
        private readonly List<(long, string)> _sent;
        private readonly IReadOnlyCollection<long> _failing;

        public RecordingMessenger(List<(long, string)> sent, params long[] failing)
        {
            _sent = sent;
            _failing = failing;
        }

        public Task SendAsync(long chatId, string text, CancellationToken cancellationToken)
        {
            if (_failing.Contains(chatId))
            {
                throw new InvalidOperationException("chat is blocked");
            }

            _sent.Add((chatId, text));
            return Task.CompletedTask;
        }
    }

    private static SiteUnavailableNotifier Build(
        List<(long ChatId, string Text)> sent,
        IReadOnlyCollection<long> failing,
        params long[] admins
    ) =>
        new(
            new RecordingMessenger(sent, [.. failing]),
            Options.Create(new TelegramConfig { BotToken = "token", AdminIds = admins }),
            NullLogger<SiteUnavailableNotifier>.Instance
        );

    [Fact]
    public async Task NotifyAsync_SendsMessageToEveryAdmin()
    {
        var ct = TestContext.Current.CancellationToken;
        var notifier = Build(_sent, [], FirstAdmin, SecondAdmin);

        var result = await notifier.NotifyAsync(Site, new InvalidOperationException("down"), ct);

        Assert.True(result.Success);
        Assert.Equal(2, result.Result);
        Assert.Equal([FirstAdmin, SecondAdmin], _sent.Select(s => s.ChatId));
    }

    [Fact]
    public async Task NotifyAsync_IncludesSiteAndErrorMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        var notifier = Build(_sent, [], FirstAdmin);

        await notifier.NotifyAsync(Site, new InvalidOperationException("dns is down"), ct);

        Assert.Contains("example.test", _sent.Single().Text);
        Assert.Contains("dns is down", _sent.Single().Text);
    }

    /// <summary>
    /// Ошибка отправки одному администратору не должна отменять уведомление
    /// остальных: список берётся из конфигурации, и молчаливая потеря сообщения
    /// у части админов хуже, чем частично доставленное.
    /// </summary>
    [Fact]
    public async Task NotifyAsync_Continues_WhenOneAdminFails()
    {
        var ct = TestContext.Current.CancellationToken;
        var notifier = Build(_sent, [FirstAdmin], FirstAdmin, SecondAdmin);

        var result = await notifier.NotifyAsync(Site, new InvalidOperationException("down"), ct);

        Assert.Equal(1, result.Result);
        Assert.Equal([SecondAdmin], _sent.Select(s => s.ChatId));
    }

    [Fact]
    public async Task NotifyAsync_Fails_WhenEveryAdminFails()
    {
        var ct = TestContext.Current.CancellationToken;
        var notifier = Build(_sent, [FirstAdmin], FirstAdmin);

        var result = await notifier.NotifyAsync(Site, new InvalidOperationException("down"), ct);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task NotifyAsync_Fails_WhenNoAdminsConfigured()
    {
        var ct = TestContext.Current.CancellationToken;
        var notifier = Build(_sent, []);

        var result = await notifier.NotifyAsync(Site, new InvalidOperationException("down"), ct);

        Assert.False(result.Success);
        Assert.Empty(_sent);
    }

    /// <summary>
    /// Сервис запускается и без Telegram-токена: конвейер в этом случае просто
    /// пишет предупреждение в лог, а не роняет хост.
    /// </summary>
    [Fact]
    public async Task NotifyAsync_Fails_WhenTelegramIsNotConfigured()
    {
        var ct = TestContext.Current.CancellationToken;
        var notifier = new SiteUnavailableNotifier(
            null,
            Options.Create(new TelegramConfig { BotToken = "token", AdminIds = [FirstAdmin] }),
            NullLogger<SiteUnavailableNotifier>.Instance
        );

        var result = await notifier.NotifyAsync(Site, new InvalidOperationException("down"), ct);

        Assert.False(result.Success);
        Assert.Empty(_sent);
    }
}
