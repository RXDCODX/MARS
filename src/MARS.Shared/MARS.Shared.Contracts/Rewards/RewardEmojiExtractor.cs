using System.Globalization;
using System.Text;

namespace MARS.Shared.Contracts.Rewards;

public static class RewardEmojiExtractor
{
    public static string FirstGrapheme(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var enumerator = StringInfo.GetTextElementEnumerator(value.Trim());
        return enumerator.MoveNext() ? enumerator.GetTextElement() : string.Empty;
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            if (ch != '\uFE0F')
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Trim();
    }

    public static string ExtractNormalizedLeadingEmoji(string? value) =>
        Normalize(FirstGrapheme(value));

    private static IEnumerable<string> EnumerateGraphemes(string value)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(value);
        while (enumerator.MoveNext())
        {
            yield return enumerator.GetTextElement();
        }
    }
}
