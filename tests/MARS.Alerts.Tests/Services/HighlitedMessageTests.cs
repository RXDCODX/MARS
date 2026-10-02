using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Выделение сообщения в чате.
///
/// Выделение — реакция на сообщения участников с особыми правами, и она видна всем
/// зрителям. Проверяется, что обычное сообщение не выделяется, а сообщение от
/// модератора выделяется, причём цвет подставляется по правилам шаблона.
/// </summary>
public class HighlitedMessageTests : IDisposable
{
    private const string HighlightText = "сообщение выделено";

    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly string _webRoot = Path.Combine(
        Path.GetTempPath(),
        "mars-highlight",
        Guid.NewGuid().ToString("N")
    );

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, true);
        }
    }

    /// <summary>
    /// Обычный зритель не получает выделения: иначе в чате постоянно мелькали бы
    /// плашки на каждое сообщение.
    /// </summary>
    [Fact]
    public async Task RegularViewerIsNotHighlighted()
    {
        await Create()
            .HandleHighlightedMessage(
                User(),
                "привет",
                "#FFFFFF",
                isVip: false,
                isModerator: false,
                isBroadcaster: false
            );

        await WaitForAsync(() => _notifier.Invocations.Count > 0);

        Assert.Empty(_notifier.Invocations);
    }

    /// <summary>
    /// Сообщение модератора выделяется, и зритель видит сам текст сообщения.
    /// </summary>
    [Fact]
    public async Task ModeratorMessageIsHighlighted()
    {
        await Create()
            .HandleHighlightedMessage(
                User(),
                HighlightText,
                "#FF0000",
                isVip: false,
                isModerator: true,
                isBroadcaster: false
            );

        await WaitForAsync(() => _notifier.Invocations.Count > 0);

        _notifier.Verify(
            notifier =>
                notifier.Highlite(
                    It.IsAny<object>(),
                    It.Is<string>(color => color == "#FF0000"),
                    It.IsAny<object>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Пустой цвет заменяется на белый: без него OBS показал бы невидимую плашку.
    /// </summary>
    [Fact]
    public async Task BlankColorFallsBackToWhite()
    {
        await Create()
            .HandleHighlightedMessage(
                User(),
                HighlightText,
                "   ",
                isVip: false,
                isModerator: true,
                isBroadcaster: false
            );

        await WaitForAsync(() => _notifier.Invocations.Count > 0);

        _notifier.Verify(
            notifier =>
                notifier.Highlite(
                    It.IsAny<object>(),
                    It.Is<string>(color => color == "#ffffff"),
                    It.IsAny<object>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Выделение идёт без ожидания, поэтому результат дожидается по счётчику
    /// вызовов, а не по времени сна.
    /// </summary>
    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private static TwitchUser User() =>
        new()
        {
            TwitchId = "123456789",
            UserLogin = "pyro",
            DisplayName = "Pyro",
        };

    /// <summary>
    /// RickRoll отключён нулевым шансом: иначе вместо выделения зритель получил бы
    /// ролик, а проверяется именно выделение.
    /// </summary>
    private HighlitedMessage Create()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "faces"));
        File.WriteAllText(Path.Combine(_webRoot, "faces", "reaction.png"), "png");

        var rickRoller = new RickRollerService(
            _notifier.Object,
            new ConfigurationBuilder()
                .AddInMemoryCollection([
                    new KeyValuePair<string, string?>("AppSettings:RickRoll:Chance", "0"),
                ])
                .Build()
        );

        return new HighlitedMessage(_notifier.Object, new FakeEnvironment(_webRoot), rickRoller);
    }

    private sealed class FakeEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MARS.Alerts.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Test";
    }
}
