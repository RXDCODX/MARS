namespace MARS.Telegram.Services.Booru;

/// <summary>
/// Лимит тегов в правиле автопостинга.
/// </summary>
/// <remarks>
/// Два тега, а не «сколько угодно»: выборка на booru по большому списку тегов
/// почти всегда пуста, и правило молча перестало бы публиковать.
/// </remarks>
public static class TagValidator
{
    public const int MaxTags = 2;

    public static bool IsValidTagCount(string tags, int maxTags = MaxTags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return true;
        }

        var tagCount = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return tagCount <= maxTags;
    }

    public static string? GetValidationError(string tags, int maxTags = MaxTags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        var tagCount = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (tagCount > maxTags)
        {
            return $"Максимальное количество тегов: {maxTags}. Указано: {tagCount}";
        }

        return null;
    }
}
