using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.Shared.Data;

/// <summary>
/// <see cref="UtcDateTimeConverter"/> для nullable-колонок: null остаётся null,
/// а значение приводится к UTC так же, как у обязательного типа.
/// </summary>
public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            value => value.HasValue ? UtcDateTimeConverter.ToUniversal(value.Value) : null,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null
        ) { }
}
