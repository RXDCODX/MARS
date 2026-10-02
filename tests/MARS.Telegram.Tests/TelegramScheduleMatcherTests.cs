using MARS.Telegram.Models;
using MARS.Telegram.Services.Booru;

namespace MARS.Telegram.Tests;

/// <summary>
/// Сверка расписания планировщика с отложенными сообщениями Telegram.
/// </summary>
public class TelegramScheduleMatcherTests
{
    private static readonly DateTime At = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static TelegramScheduledMessageInfo Message(int id, DateTime at) => new(id, at);

    /// <summary>
    /// Допуск в 2 минуты перенесён из монолита: WTelegram округляет время
    /// отправки, и точное сравнение заставляло бы планировщик дублировать
    /// уже отложенные сообщения.
    /// </summary>
    [Fact]
    public void Tolerance_IsTwoMinutes() =>
        Assert.Equal(TimeSpan.FromMinutes(2), TelegramScheduleMatcher.Tolerance);

    [Fact]
    public void MatchesAny_AcceptsMessageInsideTolerance()
    {
        var result = TelegramScheduleMatcher.MatchesAny(At.AddMinutes(-1.5), [At]);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAny_RejectsMessageOutsideTolerance()
    {
        var result = TelegramScheduleMatcher.MatchesAny(At.AddMinutes(-5), [At]);

        Assert.False(result);
    }

    /// <summary>
    /// Допуск двусторонний: сообщение может оказаться и позже вхождения CRON,
    /// если планировщик опоздал с проходом.
    /// </summary>
    [Fact]
    public void MatchesAny_AcceptsMessageLaterThanOccurrence()
    {
        var result = TelegramScheduleMatcher.MatchesAny(At.AddMinutes(1), [At]);

        Assert.True(result);
    }

    [Fact]
    public void CountMatches_CountsOnlyMatchingMessages()
    {
        var messages = new[] { Message(1, At), Message(2, At.AddHours(3)) };

        var result = TelegramScheduleMatcher.CountMatches(messages, [At]);

        Assert.Equal(1, result);
    }

    [Fact]
    public void FindEarliestMatch_ReturnsTheEarliestScheduledMessage()
    {
        var messages = new[] { Message(1, At.AddHours(2)), Message(2, At.AddMinutes(1)) };

        var result = TelegramScheduleMatcher.FindEarliestMatch(messages, [At]);

        Assert.Equal(At.AddMinutes(1), result);
    }

    [Fact]
    public void FindEarliestMatch_ReturnsNull_WhenNothingMatches()
    {
        var messages = new[] { Message(1, At.AddHours(3)) };

        var result = TelegramScheduleMatcher.FindEarliestMatch(messages, [At]);

        Assert.Null(result);
    }

    [Fact]
    public void FindMissingOccurrences_ReportsUnplannedSlots()
    {
        var messages = new[] { Message(1, At) };

        var result = TelegramScheduleMatcher.FindMissingOccurrences(messages, [At, At.AddHours(1)]);

        Assert.Equal([At.AddHours(1)], result);
    }

    [Fact]
    public void FindUnmatchedMessages_ReportsMessagesWithoutOccurrence()
    {
        var messages = new[] { Message(1, At), Message(2, At.AddHours(3)) };

        var result = TelegramScheduleMatcher.FindUnmatchedMessages(messages, [At]);

        Assert.Equal([2], result.Select(m => m.MessageId));
    }
}
