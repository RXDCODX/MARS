using MARS.Telegram.Services.Booru;

namespace MARS.Telegram.Tests;

/// <summary>
/// Проверка полей правила автопостинга.
/// </summary>
public class BooruValidationHelperTests
{
    [Fact]
    public void ValidateAndParseDiscordChannelId_ReturnsNoError_ForNumericValue()
    {
        var error = BooruValidationHelper.ValidateAndParseDiscordChannelId(
            "1234567890",
            out var parsed
        );

        Assert.Null(error);
        Assert.Equal(1234567890UL, parsed);
    }

    /// <summary>
    /// Нулевой идентификатор канала проходит <c>TryParse</c>, но отправлять
    /// в него нельзя, поэтому он отвергается вместе с нечисловым значением.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-number")]
    [InlineData("0")]
    public void ValidateAndParseDiscordChannelId_RejectsUnusableValue(string raw)
    {
        var error = BooruValidationHelper.ValidateAndParseDiscordChannelId(raw, out var parsed);

        Assert.NotNull(error);
        Assert.Equal(0UL, parsed);
    }

    [Fact]
    public void ValidateAndParseDiscordChannelId_UsesGivenFieldName()
    {
        var error = BooruValidationHelper.ValidateAndParseDiscordChannelId(
            "",
            out _,
            "PostConfigId"
        );

        Assert.Contains("PostConfigId", error);
    }

    /// <summary>
    /// Каналы Telegram отрицательные: именно такие идентификаторы приходят от
    /// API, поэтому проверка обязана их принимать.
    /// </summary>
    [Fact]
    public void ValidateAndParseTelegramChannelId_AcceptsNegativeChannelId()
    {
        var error = BooruValidationHelper.ValidateAndParseTelegramChannelId(
            "-1001234567890",
            out var parsed
        );

        Assert.Null(error);
        Assert.Equal(-1001234567890L, parsed);
    }

    [Fact]
    public void ValidateAndParseTelegramChannelId_RejectsEmptyValue()
    {
        var error = BooruValidationHelper.ValidateAndParseTelegramChannelId(" ", out var parsed);

        Assert.NotNull(error);
        Assert.Equal(0L, parsed);
    }

    [Theory]
    [InlineData("0 5 * * *")]
    [InlineData("*/15 * * * *")]
    public void ValidateCronExpression_AcceptsValidExpression(string expression)
    {
        var error = BooruValidationHelper.ValidateCronExpression(expression);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("не cron вовсе")]
    public void ValidateCronExpression_RejectsUnusableExpression(string? expression)
    {
        var error = BooruValidationHelper.ValidateCronExpression(expression);

        Assert.NotNull(error);
    }
}
