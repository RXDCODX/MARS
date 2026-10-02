namespace MARS.Telegram.Services.Booru;

/// <summary>
/// Подстановка переменных в шаблон текста публикации.
/// </summary>
/// <remarks>
/// Сравнение имён без учёта регистра перенесено из монолита: разметка приходила
/// от людей, и <c>{tags}</c> с <c>{Tags}</c> в одном шаблоне не должна была
/// молча превращаться в пустое место.
/// </remarks>
public static class BooruMessageTemplateResolver
{
    public static string Resolve(string template, Dictionary<string, string?> variables)
    {
        var result = template;

        foreach (var (key, value) in variables)
        {
            result = result.Replace(
                $"{{{key}}}",
                value ?? string.Empty,
                StringComparison.OrdinalIgnoreCase
            );
        }

        return result;
    }
}
