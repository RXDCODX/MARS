using MARS.Telegram.Services.Booru;

namespace MARS.Telegram.Tests;

/// <summary>
/// Подстановка переменных в шаблон текста публикации.
/// </summary>
public class BooruMessageTemplateResolverTests
{
    [Fact]
    public void Resolve_ReplacesEveryOccurrence()
    {
        var result = BooruMessageTemplateResolver.Resolve(
            "{tags} — {tags} ещё раз",
            new Dictionary<string, string?> { ["tags"] = "waifu" }
        );

        Assert.Equal("waifu — waifu ещё раз", result);
    }

    /// <summary>
    /// Регистр не важен: разметку приходилось править руками, и <c>{Tags}</c>
    /// молча превращался в пустое место только потому, что был написан иначе.
    /// </summary>
    [Fact]
    public void Resolve_IgnoresVariableNameCase()
    {
        var result = BooruMessageTemplateResolver.Resolve(
            "Смотри: {Tags}",
            new Dictionary<string, string?> { ["tags"] = "waifu" }
        );

        Assert.Equal("Смотри: waifu", result);
    }

    [Fact]
    public void Resolve_TreatsNullValueAsEmptyString()
    {
        var result = BooruMessageTemplateResolver.Resolve(
            "[{source}]",
            new Dictionary<string, string?> { ["source"] = null }
        );

        Assert.Equal("[]", result);
    }

    [Fact]
    public void Resolve_KeepsUnknownPlaceholders()
    {
        var result = BooruMessageTemplateResolver.Resolve(
            "{tags} {unknown}",
            new Dictionary<string, string?> { ["tags"] = "waifu" }
        );

        Assert.Equal("waifu {unknown}", result);
    }
}
