using MARS.TwitchCore.Services.HelloVideos;

namespace MARS.Microservices.Tests.HelloVideos;

public class HelloVideoEligibilityTests
{
    [Fact]
    public void ShouldNotify_WhenSameDay_ReturnsFalse()
    {
        var last = new DateTime(2026, 3, 14, 10, 0, 0);
        var now = new DateTime(2026, 3, 14, 23, 59, 0);

        var result = HelloVideoEligibility.ShouldNotify(last, now);

        Assert.False(result);
    }

    [Fact]
    public void ShouldNotify_WhenNextDay_ReturnsTrue()
    {
        var last = new DateTime(2026, 3, 14, 23, 59, 0);
        var now = new DateTime(2026, 3, 15, 0, 1, 0);

        var result = HelloVideoEligibility.ShouldNotify(last, now);

        Assert.True(result);
    }

    [Fact]
    public void ShouldNotify_WhenSameDayNumberInDifferentMonth_ReturnsTrue()
    {
        // Регрессия аудита №17: сравнение только .Day подавляло приветствие
        // 1-го числа каждого месяца, потому что 1-е число прошлого месяца
        // формально «тот же день».
        var last = new DateTime(2026, 2, 1, 8, 0, 0);
        var now = new DateTime(2026, 3, 1, 9, 0, 0);

        var result = HelloVideoEligibility.ShouldNotify(last, now);

        Assert.True(result);
    }

    [Fact]
    public void ShouldNotify_WhenSameDayNumberInDifferentYear_ReturnsTrue()
    {
        var last = new DateTime(2025, 12, 25, 8, 0, 0);
        var now = new DateTime(2026, 12, 25, 9, 0, 0);

        var result = HelloVideoEligibility.ShouldNotify(last, now);

        Assert.True(result);
    }

    [Fact]
    public void ShouldNotify_WhenLastNotificationIsInFuture_ReturnsTrue()
    {
        // Часы перевели/разъехались — не блокируем пользователя навсегда.
        var last = new DateTime(2026, 3, 20, 10, 0, 0);
        var now = new DateTime(2026, 3, 14, 10, 0, 0);

        var result = HelloVideoEligibility.ShouldNotify(last, now);

        Assert.True(result);
    }
}
