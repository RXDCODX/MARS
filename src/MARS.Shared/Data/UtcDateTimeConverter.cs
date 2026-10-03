using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.Shared.Data;

/// <summary>
/// Приводит <see cref="DateTime"/> к UTC перед записью в базу.
/// </summary>
/// <remarks>
/// Npgsql начиная с 6-й версии отвергает <see cref="DateTimeKind.Local"/> при
/// записи в <c>timestamp with time zone</c>: хранить локальное время без зоны
/// нельзя, а молча записать его как UTC значило бы сдвинуть момент времени.
/// В коде сервисов <c>DateTime.Now</c> встречается сотни раз, и переписывать их
/// на <c>UtcNow</c> — значит разъехаться с местом, где время только читают и
/// сравнивают. Поэтому приведение делается один раз, на границе с базой:
/// <para>
/// — <see cref="DateTimeKind.Local"/> переводится в тот же момент времени в UTC;
/// это не сдвиг, а честное представление «сейчас»;</para>
/// — <see cref="DateTimeKind.Utc"/> остаётся как есть;
/// <para>
/// — <see cref="DateTimeKind.Unspecified"/> считается UTC: в базе зона всё равно
/// не хранится, а трактовка как локального вносила бы сдвиг на величину
/// смещения машины.</para>
/// <para>
/// Чтение возвращает <see cref="DateTimeKind.Utc"/>, поэтому вывод в чат или на
/// OBS должен явно звать <c>ToLocalTime()</c> — иначе зритель увидит UTC.
/// </para>
/// </remarks>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => ToUniversal(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    { }

    internal static DateTime ToUniversal(DateTime value)
    {
        var result = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return result;
    }
}
