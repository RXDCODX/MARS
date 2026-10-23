using MARS.Telegram.Services.Logging;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Лимит частоты доставки журнала.
///
/// Лимит считается на стороне Telegram, а не на стороне отправителя: там
/// видны все сервисы сразу, и только одна точка может решить, что чат
/// наполняется слишком быстро. Считается он на сервис — иначе один
/// зашумевший сервис молча отрезал бы журнал остальным.
///
/// Счётчик отброшенных записей не теряется: иначе потеря выглядела бы как
/// «доставилось всё».
/// </summary>
public class TelegramLogRateLimiterTests
{
    private static readonly DateTimeOffset Начало = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Записи до лимита проходят.
    /// </summary>
    [Fact]
    public void Записи_До_Лимита_Проходят()
    {
        var limiter = new TelegramLogRateLimiter(3);

        for (var index = 0; index < 3; index++)
        {
            var decision = limiter.TryAcquire("MARS.TwitchCore", Начало);

            Assert.True(decision.Allowed);
            Assert.Equal(0, decision.Suppressed);
        }
    }

    /// <summary>
    /// Сверх лимита запись отбрасывается: иначе один сервис, ломающийся
    /// тысячу раз в секунду, забил бы чат и вытеснил журнал остальных.
    /// </summary>
    [Fact]
    public void Записи_Сверх_Лимита_Отбрасываются()
    {
        var limiter = new TelegramLogRateLimiter(2);

        limiter.TryAcquire("MARS.TwitchCore", Начало);
        limiter.TryAcquire("MARS.TwitchCore", Начало);

        var decision = limiter.TryAcquire("MARS.TwitchCore", Начало);

        Assert.False(decision.Allowed);
    }

    /// <summary>
    /// Минута прошла — лимит снова доступен: ограничение ограничивает поток,
    /// а не работу сервиса.
    /// </summary>
    [Fact]
    public void Через_Минуту_Лимит_Снова_Доступен()
    {
        var limiter = new TelegramLogRateLimiter(1);

        limiter.TryAcquire("MARS.TwitchCore", Начало);
        Assert.False(limiter.TryAcquire("MARS.TwitchCore", Начало).Allowed);

        var decision = limiter.TryAcquire("MARS.TwitchCore", Начало.AddMinutes(1));

        Assert.True(decision.Allowed);
    }

    /// <summary>
    /// Лимит отдельный для каждого сервиса: шумный сервис не должен отрезать
    /// журнал тихим.
    /// </summary>
    [Fact]
    public void Лимит_Отдельный_Для_Каждого_Сервиса()
    {
        var limiter = new TelegramLogRateLimiter(1);

        Assert.True(limiter.TryAcquire("MARS.TwitchCore", Начало).Allowed);
        Assert.False(limiter.TryAcquire("MARS.TwitchCore", Начало).Allowed);
        Assert.True(limiter.TryAcquire("MARS.Admin", Начало).Allowed);
    }

    /// <summary>
    /// Отброшенные записи считаются и едут со следующей принятой: потеря
    /// должна быть видна, а не молчаливой.
    /// </summary>
    [Fact]
    public void Отброшенные_Записи_Считаются_И_Возвращаются_Следующей()
    {
        var limiter = new TelegramLogRateLimiter(1);

        limiter.TryAcquire("MARS.TwitchCore", Начало);
        limiter.TryAcquire("MARS.TwitchCore", Начало);
        var dropped = limiter.TryAcquire("MARS.TwitchCore", Начало);

        Assert.False(dropped.Allowed);
        Assert.Equal(2, dropped.Suppressed);

        var next = limiter.TryAcquire("MARS.TwitchCore", Начало.AddMinutes(1));

        Assert.True(next.Allowed);
        Assert.Equal(2, next.Suppressed);
    }

    /// <summary>
    /// Нулевой лимит — это «не отбрасывать ничего», а не «не пропускать
    /// ничего»: иначе опечатка в конфигурации молча отключила бы журнал.
    /// </summary>
    [Fact]
    public void Нулевой_Лимит_Не_Отбрасывает_Записи()
    {
        var limiter = new TelegramLogRateLimiter(0);

        Assert.True(limiter.TryAcquire("MARS.TwitchCore", Начало).Allowed);
        Assert.True(limiter.TryAcquire("MARS.TwitchCore", Начало).Allowed);
    }
}
