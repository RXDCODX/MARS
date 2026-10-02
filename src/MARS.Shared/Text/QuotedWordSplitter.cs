using System.Text;
using System.Text.RegularExpressions;

namespace MARS.Shared.Text;

/// <summary>
/// Разбиение триггерного слова на токены с учётом кавычек.
/// </summary>
/// <remarks>
/// Перенос <c>StringExtension.SplitWithQuotes</c> из монолита. Отличие одно:
/// вместо <c>Exception("ты насрал в ковычках")</c> возвращается <c>null</c> —
/// вызывающая сторона решает, что делать с испорченным триггером, а не падает.
/// Кавычки нужны для фраз: <c>"добрый вечер"</c> обязан остаться одним
/// токеном, иначе подстрока никогда не совпадёт.
/// </remarks>
public static class QuotedWordSplitter
{
    /// <summary>
    /// Токены строки. <c>null</c>, если кавычка не закрыта.
    /// </summary>
    public static string[]? SplitWithQuotes(this string input)
    {
        var text = Regex.Replace(input.Trim(), @"\s+", " ");
        var list = new List<string>();
        var builder = new StringBuilder();
        var isQuoted = false;

        foreach (var character in text)
        {
            if (character == '"')
            {
                isQuoted = !isQuoted;

                if (!isQuoted && builder.Length > 0)
                {
                    list.Add(builder.ToString());
                    builder.Clear();
                }

                continue;
            }

            if (character == ' ' && !isQuoted)
            {
                if (builder.Length > 0)
                {
                    list.Add(builder.ToString());
                    builder.Clear();
                }

                continue;
            }

            builder.Append(character);
        }

        if (isQuoted)
        {
            return null;
        }

        // Последний токен не закрывается пробелом, поэтому дописывается вручную.
        if (builder.Length > 0)
        {
            list.Add(builder.ToString());
        }

        return [.. list];
    }
}
