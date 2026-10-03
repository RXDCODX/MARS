using MARS.Shared.Data;

namespace MARS.Shared.Tests.Data;

/// <summary>
/// Приведение дат к UTC на границе с базой.
/// </summary>
/// <remarks>
/// Правило общее для репозитория, и оно неочевидно в одной важной части:
/// <see cref="DateTimeKind.Local"/> переводится в тот же момент времени, а не
/// переименовывается в UTC. Переименование хранило бы «сейчас» со сдвигом на
/// смещение машины, и на стенде с TZ=UTC это осталось бы незаметным.
/// </remarks>
public class UtcDateTimeConverterTests
{
    /// <summary>
    /// Локальное время переводится в тот же момент в UTC: иначе запись «сейчас»
    /// хранилась бы со сдвигом на смещение машины.
    /// </summary>
    [Fact]
    public void LocalTimeKeepsTheSameInstant()
    {
        var local = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Local);
        var converter = new UtcDateTimeConverter();

        var stored = (DateTime)converter.ConvertToProvider(local)!;

        Assert.Equal(DateTimeKind.Utc, stored.Kind);
        Assert.Equal(local.ToUniversalTime(), stored);
    }

    /// <summary>
    /// Время в UTC не меняется: повторный перевод не должен двигать момент.
    /// </summary>
    [Fact]
    public void UtcTimeIsUnchanged()
    {
        var value = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var converter = new UtcDateTimeConverter();

        Assert.Equal(value, (DateTime)converter.ConvertToProvider(value)!);
    }

    /// <summary>
    /// Время без зоны считается UTC: в базе зона всё равно не хранится, а
    /// прочтение как локального внесло бы тот же сдвиг, что и переименование.
    /// </summary>
    [Fact]
    public void UnspecifiedTimeIsTreatedAsUtc()
    {
        var value = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var converter = new UtcDateTimeConverter();

        var stored = (DateTime)converter.ConvertToProvider(value)!;

        Assert.Equal(DateTimeKind.Utc, stored.Kind);
        Assert.Equal(value, stored);
    }

    /// <summary>
    /// Из базы значение приходит с меткой UTC: код, который печатает время, знает
    /// об этом и может звать ToLocalTime() там, где это нужно зрителю.
    /// </summary>
    [Fact]
    public void ValueFromDatabaseIsMarkedAsUtc()
    {
        var value = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var converter = new UtcDateTimeConverter();

        var read = (DateTime)converter.ConvertFromProvider(value)!;

        Assert.Equal(DateTimeKind.Utc, read.Kind);
        Assert.Equal(value, read);
    }

    /// <summary>
    /// Пустое значение остаётся пустым: приведение не превращает null в нулевое
    /// время, из-за чего незаполненные даты выглядели бы заполненными.
    /// </summary>
    [Fact]
    public void EmptyValueStaysEmpty()
    {
        var converter = new NullableUtcDateTimeConverter();

        Assert.Null((DateTime?)converter.ConvertToProvider(null));
        Assert.Null((DateTime?)converter.ConvertFromProvider(null));
    }

    /// <summary>
    /// Непустое значение nullable-колонки приводится так же, как обязательного:
    /// две разные настройки для одного правила разошлись бы при добавлении
    /// колонки.
    /// </summary>
    [Fact]
    public void EmptyColumnKeepsUtcConversionOfTheSharedRule()
    {
        var local = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Local);
        var converter = new NullableUtcDateTimeConverter();

        var stored = (DateTime?)converter.ConvertToProvider(local);

        Assert.NotNull(stored);
        Assert.Equal(DateTimeKind.Utc, stored.Value.Kind);
        Assert.Equal(local.ToUniversalTime(), stored.Value);
    }
}
