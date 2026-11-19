using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Alerts.Application.Twitch.Rewards.Services;

using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
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

        await Task.CompletedTask;
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

        await Task.CompletedTask;
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

        await Task.CompletedTask;
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
            new ConfigurationBuilder()
                .AddInMemoryCollection([
                    new KeyValuePair<string, string?>("AppSettings:RickRoll:Chance", "0"),
                ])
                .Build()
        );

        return new HighlitedMessage(new FakeEnvironment(_webRoot), rickRoller);
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
