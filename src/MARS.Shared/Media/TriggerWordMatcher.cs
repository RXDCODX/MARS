using System.Text.RegularExpressions;
using MARS.Shared.Models.Media;
using MARS.Shared.Text;

namespace MARS.Shared.Media;

/// <summary>
/// Подбор алертов по ключевым словам из сообщения чата.
/// </summary>
/// <remarks>
/// Правила перенесены из <c>TwitchMessagesHubAwaker.ClientKeyTriggerAlert</c> один
/// в один, включая две особенности, которые выглядят как опечатки, но являются
/// поведением:
/// <list type="bullet">
/// <item>регуляркой считается всё, что <c>new Regex</c> смог собрать, — то есть
/// и обычное слово; границы слова добавляются автоматически, если триггер не
/// начинается с <c>\b</c>/<c>^</c> и не кончается на <c>\b</c>/<c>$</c>;</item>
/// <item>нескомпилировавшийся токен сравнивается как обычное слово, а фраза с
/// пробелом дополнительно ищется подстрокой.</item>
/// </list>
/// Регулярные выражения собираются с <see cref="RegexOptions.NonBacktracking"/>:
/// текст из чата не должен быть поводом для ReDoS. Токены, которые такой движок
/// не поддерживает (ретроспективные проверки, обратные ссылки), пропускаются —
/// в монолите они роняли весь проход по алертам.
/// </remarks>
public static class TriggerWordMatcher
{
    private const RegexOptions RegexOptions =
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking;

    /// <summary>
    /// Возвращает алерты, у которых хотя бы один триггер совпал с сообщением.
    /// Каждый алерт возвращается один раз: в монолите сработавший дважды алерт
    /// попадал в список дважды и становился вдвое вероятнее в розыгрыше.
    /// </summary>
    /// <param name="alerts">Кандидаты: у каждого проверяется <c>MetaInfo.IsEnabled</c>.</param>
    /// <param name="message">Текст сообщения чата.</param>
    public static IReadOnlyList<MediaInfo> Match(IEnumerable<MediaInfo> alerts, string message)
    {
        var result = new List<MediaInfo>();

        if (string.IsNullOrWhiteSpace(message))
        {
            return result;
        }

        var text = message.Trim();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var alert in alerts)
        {
            var trigger = alert.TextInfo.TriggerWord;

            if (alert.MetaInfo.IsEnabled && !string.IsNullOrWhiteSpace(trigger))
            {
                var triggers = trigger.SplitWithQuotes();

                if (triggers is null)
                {
                    continue;
                }

                if (triggers.Any(pattern => Matches(text, words, pattern)))
                {
                    result.Add(alert);
                }
            }
        }

        return result;
    }

    private static bool Matches(string message, string[] words, string pattern)
    {
        if (IsValidRegex(pattern))
        {
            return MatchesRegex(message, words, pattern);
        }

        if (
            words.Any(word => word.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            || pattern.Contains(' ')
                && message.Contains(pattern, StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        return false;
    }

    private static bool MatchesRegex(string message, string[] words, string pattern)
    {
        var expression = pattern;

        if (
            !expression.StartsWith("\\b", StringComparison.Ordinal)
            && !expression.StartsWith("^", StringComparison.Ordinal)
        )
        {
            expression = "\\b" + expression;
        }

        if (
            !expression.EndsWith("\\b", StringComparison.Ordinal)
            && !expression.EndsWith("$", StringComparison.Ordinal)
        )
        {
            expression += "\\b";
        }

        var isAnchored =
            expression.StartsWith("^", StringComparison.Ordinal)
            || expression.EndsWith("$", StringComparison.Ordinal);

        if (IsMatch(message, expression))
        {
            return true;
        }

        return !isAnchored && words.Any(word => IsMatch(word, expression));
    }

    /// <summary>
    /// Движок NonBacktracking не поддерживает часть конструкций обычного
    /// регулярного выражения: <c>new Regex</c> такой триггер собирает, а
    /// NonBacktracking бросает <see cref="NotSupportedException"/>. Ловить надо
    /// оба класса — иначе испорченный триггер обрушивал бы обработку сообщения.
    /// </summary>
    private static bool IsMatch(string input, string expression)
    {
        try
        {
            return Regex.IsMatch(input, expression, RegexOptions);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Выражение считается регулярным, если <c>new Regex</c> его собирает: так же
    /// и в монолите, где проверялось только то, что конструкция не падает.
    /// </summary>
    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
            return !string.IsNullOrWhiteSpace(pattern);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
