using MARS.Telegram.Services.Booru;

namespace MARS.Telegram.Tests;

/// <summary>
/// Лимит тегов в правиле автопостинга.
/// </summary>
public class TagValidatorTests
{
    [Fact]
    public void MaxTags_IsTwo() => Assert.Equal(2, TagValidator.MaxTags);

    /// <summary>
    /// Пустой список тегов допустим: правило без тегов означает «любая выборка»,
    /// и его отклонение сделало бы часть конфигураций непроверяемыми.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidTagCount_AcceptsEmptyTagList(string tags)
    {
        Assert.True(TagValidator.IsValidTagCount(tags));
        Assert.Null(TagValidator.GetValidationError(tags));
    }

    [Fact]
    public void IsValidTagCount_AcceptsTagsUpToTheLimit()
    {
        Assert.True(TagValidator.IsValidTagCount("waifu  cat"));
    }

    /// <summary>
    /// Считаются только непустые элементы: два тега, разделённые пробелами подряд,
    /// не должны превращаться в четыре.
    /// </summary>
    [Fact]
    public void IsValidTagCount_IgnoresExtraSpaces()
    {
        Assert.True(TagValidator.IsValidTagCount("waifu   cat"));
    }

    [Fact]
    public void IsValidTagCount_RejectsMoreThanTheLimit()
    {
        var tags = "one two three";

        Assert.False(TagValidator.IsValidTagCount(tags));
        Assert.Equal(
            "Максимальное количество тегов: 2. Указано: 3",
            TagValidator.GetValidationError(tags)
        );
    }

    [Fact]
    public void IsValidTagCount_HonoursCustomLimit()
    {
        Assert.True(TagValidator.IsValidTagCount("one two three", 3));
        Assert.False(TagValidator.IsValidTagCount("one two three four", 3));
    }
}
