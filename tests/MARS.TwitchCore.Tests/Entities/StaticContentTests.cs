using System.Reflection;
using MARS.TwitchCore.Entities.Subs;

namespace MARS.TwitchCore.Tests.Entities;

/// <summary>
/// Тексты мини-игр: сообщение о выбывании и фраза победителя.
///
/// Оба набора случайны, поэтому проверяется не конкретная фраза, а контракт:
/// имя игрока подставляется в текст, а у выбывания есть вариант без имени.
/// Без второй проверки набор из девяти строк мог бы «забыть» про {0} и зритель
/// увидел бы сообщение без участника.
/// </summary>
public class StaticContentTests
{
    [Fact]
    public void EliminationMessageContainsPlayerName()
    {
        var messages = Enumerable
            .Range(0, 30)
            .Select(_ => Invoke("PlayerEliminated", "Pyro"))
            .ToArray();

        Assert.All(messages, message => Assert.Contains("Pyro", message));
        Assert.All(messages, message => Assert.DoesNotContain("{0}", message));
    }

    /// <summary>
    /// Тексты действительно чередуются: из девяти строк зритель не должен
    /// получать одну и ту же фразу весь раунд.
    /// </summary>
    [Fact]
    public void EliminationMessagesVary()
    {
        var messages = Enumerable
            .Range(0, 60)
            .Select(_ => Invoke("PlayerEliminated", "Pyro"))
            .Distinct()
            .ToArray();

        Assert.True(messages.Length > 1, "все сообщения об выбывании оказались одинаковыми");
    }

    [Fact]
    public void WinnerHistoryContainsWinnerName()
    {
        var history = Invoke("GetMiniHistory", "Pyro");

        Assert.StartsWith("Pyro смог победить в игре!", history);
    }

    [Fact]
    public void WinnerHistoriesVary()
    {
        var histories = Enumerable
            .Range(0, 40)
            .Select(_ => Invoke("GetMiniHistory", "Pyro"))
            .Distinct()
            .ToArray();

        Assert.True(histories.Length > 1, "все фразы победителя оказались одинаковыми");
    }

    /// <summary>
    /// Методы внутренние: вызываются так же, как из самой игры, — рефлексией.
    /// </summary>
    private static string Invoke(string name, string argument) =>
        (string)
            typeof(StaticContent)
                .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [argument])!;
}
