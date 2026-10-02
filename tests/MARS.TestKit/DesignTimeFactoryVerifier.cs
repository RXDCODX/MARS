using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MARS.TestKit;

/// <summary>
/// Фабрики контекста для времени разработки собираются и возвращают контекст.
///
/// Фабрика нужна для <c>dotnet ef migrations add</c>, а её поломка не видна до
/// того момента, когда разработчик запустит команду вручную. Проверять надо
/// ровно две вещи: фабрика вообще создаётся и возвращает контекст с
/// настроенным провайдером — подключения она не делает, поэтому проверка
/// безопасна и не ходит в сеть.
/// </summary>
public static class DesignTimeFactoryVerifier
{
    /// <summary>
    /// Прогоняет все <see cref="IDesignTimeDbContextFactory{TContext}"/> сборки.
    /// Возвращает список неудач; пустой — успех.
    /// </summary>
    public static IReadOnlyList<string> Verify(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var failures = new List<string>();
        var checkedFactories = 0;

        foreach (
            var type in assembly
                .GetExportedTypes()
                .Where(type =>
                    !type.IsAbstract
                    && type.GetInterfaces()
                        .Any(@interface =>
                            @interface.IsGenericType
                            && @interface.GetGenericTypeDefinition()
                                == typeof(IDesignTimeDbContextFactory<>)
                        )
                )
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
        )
        {
            checkedFactories++;

            var failure = Check(type);
            if (failure is not null)
            {
                failures.Add(failure);
            }
        }

        if (checkedFactories == 0)
        {
            failures.Add($"{assemblyName}: фабрик контекста для scaffold-миграций не найдено.");
        }

        return failures;
    }

    private static string? Check(Type type)
    {
        string? failure = null;

        try
        {
            var factory = Activator.CreateInstance(type);
            if (factory is null)
            {
                failure = $"{type.Name}: не создалась.";
            }
            else
            {
                var method = type.GetMethod(
                    "CreateDbContext",
                    BindingFlags.Public | BindingFlags.Instance
                );
                if (method is null)
                {
                    failure = $"{type.Name}: нет метода CreateDbContext.";
                }
                else
                {
                    var context = method.Invoke(factory, [Array.Empty<string>()]) as DbContext;
                    if (context is null)
                    {
                        failure = $"{type.Name}: CreateDbContext вернул null.";
                    }
                    else
                    {
                        if (!context.Database.IsRelational())
                        {
                            failure =
                                $"{type.Name}: провайдер не реляционный — миграции не создадутся.";
                        }

                        context.Dispose();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            failure = exception is TargetInvocationException { InnerException: { } inner }
                ? $"{type.Name}: {inner.GetType().Name}: {inner.Message.Split('\n')[0]}"
                : $"{type.Name}: {exception.GetType().Name}: {exception.Message.Split('\n')[0]}";
        }

        return failure;
    }
}
