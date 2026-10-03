using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MARS.Shared.Data;

/// <summary>
/// Одно место, где всем контекстам сервисов назначается приведение дат к UTC.
/// </summary>
/// <remarks>
/// Правило общее для репозитория: и сервисы, и тестовая фабрика обязаны собирать
/// контекст одинаково. Иначе тест на PostgreSQL проверял бы приведение, которого
/// нет в стенде, и расхождение всплыло бы уже на развёртывании.
/// <para>
/// Применяется переопределением <c>ConfigureConventions</c> в каждом
/// контексте: единственной точки настройки модели у EF нет, а забытый вызов
/// ловится контрактным тестом на все контексты репозитория.
/// </para>
/// </remarks>
public static class MarsUtcDates
{
    /// <summary>
    /// Включает приведение <c>DateTime</c> и <c>DateTime?</c> к UTC для всех
    /// свойств модели.
    /// </summary>
    public static ModelConfigurationBuilder ConfigureUtcDates(
        this ModelConfigurationBuilder builder
    )
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();

        return builder;
    }
}
