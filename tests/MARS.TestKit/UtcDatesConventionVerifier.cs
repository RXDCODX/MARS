using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MARS.TestKit;

/// <summary>
/// Контексты сервисов обязаны приводить даты к UTC.
///
/// Npgsql начиная с 6-й версии отвергает <see cref="DateTimeKind.Local"/> при
/// записи в <c>timestamp with time zone</c>, а в коде сервисов
/// <c>DateTime.Now</c> встречается сотни раз. Правило применили шесть контекстов
/// из одиннадцати, и падение выдавал только SoundRequest — да и то лишь потому,
/// что он один ходил в живой PostgreSQL.
///
/// Отдельного вызова в <c>OnModelCreating</c> не требуется: конвенция
/// назначается переопределением <c>ConfigureConventions</c>, и вот его-то забыть
/// можно. Тест ловит именно такую забывчивость, обойдя все контексты сборки.
/// </summary>
public static class UtcDatesConventionVerifier
{
    /// <summary>
    /// Проверяет, что у каждого контекста сборки даты приводятся к UTC.
    /// Возвращает список неудач; пустой — успех.
    /// </summary>
    public static IReadOnlyList<string> Verify(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var failures = new List<string>();
        var checkedContexts = 0;

        foreach (
            var type in assembly
                .GetExportedTypes()
                .Where(type => !type.IsAbstract && typeof(DbContext).IsAssignableFrom(type))
                .Where(type => HasOptionsConstructor(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
        )
        {
            checkedContexts++;

            var failure = Check(type);
            if (failure is not null)
            {
                failures.Add(failure);
            }
        }

        if (checkedContexts == 0)
        {
            failures.Add($"{assemblyName}: контекстов данных не найдено.");
        }

        return failures;
    }

    /// <summary>
    /// Контекст собирается через <c>DbContextOptions&lt;T&gt;</c>, и тип у каждого
    /// свой: угадать его нельзя, поэтому конструктор ищется по признаку.
    /// </summary>
    private static bool HasOptionsConstructor(Type type)
    {
        return type.GetConstructors()
            .Any(constructor =>
            {
                var parameters = constructor.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType.IsGenericType;
            });
    }

    private static DbContext CreateContext(Type type)
    {
        // Модель собирается на том же провайдере, что и стенд, но подключение не
        // открывается: проверяется набор правил, а не работа с сервером.
        // Обходной провайдер здесь не годится — он и есть причина проверки.
        const string connectionString =
            "Host=localhost;Port=1;Database=none;Username=none;Password=none";

        var build = typeof(UtcDatesConventionVerifier)
            .GetMethod(nameof(BuildOptions), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type);

        var options = build.Invoke(null, [connectionString])!;
        return (DbContext)Activator.CreateInstance(type, options)!;
    }

    private static DbContextOptions<TContext> BuildOptions<TContext>(string connectionString)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        builder.UseNpgsql(connectionString);

        return builder.Options;
    }

    private static string? Check(Type type)
    {
        try
        {
            var context = CreateContext(type);

            var withoutConverter = context
                .Model.GetEntityTypes()
                .SelectMany(entity => entity.GetProperties())
                .Where(property =>
                    (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    && property.GetValueConverter() is null
                )
                .Select(property =>
                    $"{type.Name}.{property.DeclaringType.ShortName()}.{property.Name}"
                )
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            if (withoutConverter.Length > 0)
            {
                return $"{type.FullName}: даты без приведения к UTC — {string.Join(", ", withoutConverter)}";
            }

            return null;
        }
        catch (Exception ex)
        {
            // Модель строится без подключения: проверяется только набор правил, а
            // не работа с сервером. Ошибка здесь — тоже отсутствие правила.
            return $"{type.FullName}: модель не собралась ({ex.GetType().Name}: {ex.Message}).";
        }
    }
}
