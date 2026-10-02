using MARS.Shikimori.Services;

namespace MARS.Shikimori.Tests;

/// <summary>
/// Рейт-лимитер Shikimori: не больше пяти запросов в секунду, не больше
/// девяноста в минуту и не больше десяти одновременных.
///
/// Проверяется на живых часах: правило «пять в секунду» иначе не проверить —
/// подмена времени делала бы тест бессодержательным.
/// </summary>
public class ShikimoriRateLimiterTests
{
    [Fact]
    public async Task FreshLimiterHasFullQuota()
    {
        var limiter = new ShikimoriRateLimiter();

        var info = limiter.GetInfo();

        Assert.Equal(5, info.AvailablePerSecond);
        Assert.Equal(90, info.AvailablePerMinute);
        Assert.Equal(TimeSpan.Zero, info.TimeToResetSecond);
        Assert.Equal(TimeSpan.Zero, info.TimeToResetMinute);
    }

    [Fact]
    public async Task AcquireConsumesQuota()
    {
        var limiter = new ShikimoriRateLimiter();

        Assert.True(await limiter.TryAcquireAsync());
        Assert.True(await limiter.TryAcquireAsync());

        Assert.Equal(3, limiter.GetInfo().AvailablePerSecond);
        Assert.Equal(88, limiter.GetInfo().AvailablePerMinute);
    }

    /// <summary>
    /// Шестой запрос в ту же секунду не выдаётся: клиент обязан ждать, а не
    /// упираться в 429 от Shikimori и получать бан по IP.
    /// </summary>
    [Fact]
    public async Task SixthRequestInSecondIsRefused()
    {
        var limiter = new ShikimoriRateLimiter();

        var acquired = 0;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            if (await limiter.TryAcquireAsync())
            {
                acquired++;
            }
        }

        Assert.Equal(5, acquired);
        Assert.Equal(0, limiter.GetInfo().AvailablePerSecond);
        Assert.True(limiter.GetInfo().TimeToResetSecond > TimeSpan.Zero);
    }

    /// <summary>
    /// Отказ не съедает слот: иначе после пяти неудачных попыток лимитёр
    /// перестал бы отдавать слоты и поток встал бы целиком вместо того, чтобы
    /// подождать секунду.
    /// </summary>
    [Fact]
    public async Task RefusedRequestKeepsLimiterUsable()
    {
        var limiter = new ShikimoriRateLimiter();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.True(await limiter.TryAcquireAsync());
        }

        Assert.False(await limiter.TryAcquireAsync());
        Assert.Equal(0, limiter.GetInfo().AvailablePerSecond);
    }

    /// <summary>
    /// Слот параллельности обязан отпускаться, иначе он не слот, а счётчик
    /// запросов за всю жизнь сервиса: десятый вызов <c>WaitForSlotAsync</c>
    /// съел бы последний слот, а одиннадцатый ждал бы вечно. Именно это и было:
    /// интерфейс не содержит <c>Release</c>, значит освобождать обязан сам
    /// лимитёр, а вызывающий код (ShikimoriService) про это ничего не знает.
    /// </summary>
    [Fact]
    public async Task WaitForSlotDoesNotExhaustConcurrencySlots()
    {
        var limiter = new ShikimoriRateLimiter();

        for (var attempt = 0; attempt < 8; attempt++)
        {
            await limiter.WaitForSlotAsync(TestContext.Current.CancellationToken);
        }

        Assert.True(
            await limiter.TryAcquireAsync(),
            "После восьми успешных ожиданий лимитёр перестал выдавать слоты."
        );
    }

    /// <summary>
    /// Квота в секунду продолжает соблюдаться после многих вызовов: освобождение
    /// слота не должно превращать лимитёр в безлимит.
    /// </summary>
    [Fact]
    public async Task PerSecondQuotaIsStillEnforced()
    {
        var limiter = new ShikimoriRateLimiter();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.True(await limiter.TryAcquireAsync());
        }

        Assert.False(await limiter.TryAcquireAsync());
    }
}
