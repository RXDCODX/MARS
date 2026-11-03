namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Управляемые часы для лимитов и кулдаунов наград.
/// </summary>
/// <remarks>
/// Пакета <c>FakeTimeProvider</c> в тестах нет, а ждать настоящий кулдаун
/// (двадцать минут у Miku Monday) нельзя. Часы живут в UTC: лимиты считают
/// календарный день локального времени стримера, и в тесте локальная зона
/// приравнена к UTC, чтобы «следующий день» наступал по одному <see cref="Advance"/>.
/// </remarks>
internal sealed class RewardTestTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now += delta;
}
