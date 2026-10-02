using System.Text.RegularExpressions;
using MARS.Shared.Models.Media;

namespace MARS.Shared.Media;

/// <summary>
/// Подбор алертов по ключевым словам из сообщения чата.
/// </summary>
/// <remarks>
/// Правила перенесены из <c>TwitchMessagesHubAwaker</c> один в один:
/// триггер без кавычек и без <c>\b</c> обрамляется границами слова, выражение
/// считается регулярным, фразы в кавычках ищутся подстрокой. Регулярные
/// выражения собираются с <see cref="System.Text.RegularExpressions.RegexOptions.NonBacktracking"/>,
/// иначе текст из чата стал бы вектором ReDoS.
/// </remarks>
public static class TriggerWordMatcher
{
    private static readonly string[] NoWordBoundaryStarts = ["\\b", "^"];

    private static readonly string[] NoWordBoundaryEnds = ["\\b", "$"];

    /// <summary>
    /// Возвращает алерты, у которых хотя бы один триггер совпал с сообщением.
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
                var patterns = trigger.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (Matches(text, words, patterns))
                {
                    result.Add(alert);
                }
            }
        }

        return result;
    }

    private static bool Matches(string message, string[] words, string[] patterns)
    {
        var regexPatterns = new List<string>(patterns.Length);
        var plainPatterns = new List<string>(patterns.Length);

        foreach (var pattern in patterns)
        {
            if (IsRegex(pattern))
            {
                regexPatterns.Add(pattern);
            }
            else
            {
                plainPatterns.Add(pattern);
            }
        }

        var matched =
            words.Any(word =>
                plainPatterns.Any(pattern =>
                    pattern.Equals(word, StringComparison.OrdinalIgnoreCase)
                )
            ) || regexPatterns.Any(pattern => MatchesRegex(message, words, pattern));

        // Фраза в кавычках содержит пробелов и ищется подстрокой: разбивка по
        // пробелу её разорвала бы на отдельные слова.
        return matched
            || plainPatterns.Any(pattern =>
                pattern.Contains(' ')
                && message.Contains(pattern, StringComparison.OrdinalIgnoreCase)
            );
    }

    private static bool MatchesRegex(string message, string[] words, string pattern)
    {
        var anchored = pattern.StartsWith("^") || pattern.EndsWith("$");

        var expression = pattern;
        if (!NoWordBoundaryStarts.Contains(expression[..Math.Min(2, expression.Length)]))
        {
            expression = "\\b" + expression;
        }

        if (!NoWordBoundaryEnds.Contains(expression[Math.Max(0, expression.Length - 2)..]))
        {
            expression += "\\b";
        }

        if (
            Regex.IsMatch(
                message,
                expression,
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking
            )
        )
        {
            return true;
        }

        return !anchored
            && words.Any(word =>
                Regex.IsMatch(
                    word,
                    expression,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking
                )
            );
    }

    /// <summary>
    /// Выражение считается регулярным, если в нём есть обратная косая черта.
    /// Так же и в монолите: экранированное слово в триггере — это регулярка.
    /// </summary>
    private static bool IsRegex(string pattern) => pattern.Contains('\\');
}
