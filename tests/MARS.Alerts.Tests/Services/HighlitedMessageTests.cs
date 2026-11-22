using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Configuration;
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

    public void Dispose() { }

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
        var broadcaster = new TestOverlayHub();

        var rickRoller = new RickRollerService(
            new ConfigurationBuilder()
                .AddInMemoryCollection([
                    new KeyValuePair<string, string?>("AppSettings:RickRoll:Chance", "0"),
                ])
                .Build(),
            broadcaster
        );

        return new HighlitedMessage(rickRoller, broadcaster);
    }
}