using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.TestKit;

/// <summary>
/// Подстановка значения для параметра: данные, настоящий NullLogger,
/// Options.Create, loose-заглушка Moq или объект, собранный рекурсивно.
///
/// Проверяет сборку, а не поведение: «сервис собирается» — единственное, что
/// можно утверждать про заглушку. Собрать всё поведение на заглушках нельзя,
/// этим занимаются точечные тесты с настоящими зависимостями.
/// </summary>
public static class Stub
{
    /// <summary>
    /// Значение для параметра типа <paramref name="type"/> либо null, если тип
    /// трогать нельзя. <paramref name="visited"/> защищает от рекурсии по
    /// графу: класс, ссылающийся сам на себя, иначе собирался бы вечно.
    /// </summary>
    public static object? Resolve(Type type, int depth = 2, HashSet<Type>? visited = null)
    {
        var result = ResolveCore(type, depth, visited ?? new HashSet<Type>());
        return result;
    }

    private static object? ResolveCore(Type type, int depth, HashSet<Type> visited)
    {
        object? result = null;

        if (type == typeof(CancellationToken))
        {
            result = CancellationToken.None;
        }
        else if (type == typeof(ILogger))
        {
            result = NullLogger.Instance;
        }
        else if (type == typeof(IHostEnvironment) || type == typeof(IWebHostEnvironment))
        {
            result = new TestHostEnvironment();
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            result = CreateNullLogger(type.GetGenericArguments()[0]);
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOptions<>))
        {
            result = CreateOptions(type.GetGenericArguments()[0], depth, visited);
        }
        else
        {
            result = SampleValue.TryCreate(type, depth) ?? CreateComplex(type, depth, visited);
        }

        return result;
    }

    /// <summary>
    /// Интерфейс и абстрактный класс закрываются заглушкой, конкретный —
    /// собственным конструктором с подставленными аргументами.
    /// </summary>
    private static object? CreateComplex(Type type, int depth, HashSet<Type> visited)
    {
        object? instance = null;

        if (type.IsInterface || type.IsAbstract)
        {
            instance = CreateMock(type);
        }
        else if (depth > 0 && visited.Add(type))
        {
            instance = Construct(type, depth, visited);
            visited.Remove(type);
        }

        return instance;
    }

    private static object? Construct(Type type, int depth, HashSet<Type> visited)
    {
        object? instance = null;

        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(candidate => candidate.GetParameters().Length)
            .FirstOrDefault();

        if (constructor is not null)
        {
            var arguments = constructor
                .GetParameters()
                .Select(parameter => Resolve(parameter.ParameterType, depth - 1, visited))
                .ToArray();

            if (arguments.All(argument => argument is not null))
            {
                try
                {
                    instance = constructor.Invoke(arguments);
                }
                catch (Exception exception)
                {
                    _ = exception;
                    instance = null;
                }
            }
        }

        return instance;
    }

    private static object CreateNullLogger(Type category)
    {
        var loggerType = typeof(NullLogger<>).MakeGenericType(category);
        return ReadStatic(loggerType, "Instance")!;
    }

    private static object? CreateOptions(Type valueType, int depth, HashSet<Type> visited)
    {
        object? options = null;

        var value = Resolve(valueType, depth, visited);
        if (value is not null)
        {
            try
            {
                var create = typeof(Options)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(method =>
                        method.Name == nameof(Options.Create) && method.GetParameters().Length == 1
                    )
                    .MakeGenericMethod(valueType);
                options = create.Invoke(null, [value]);
            }
            catch (Exception exception)
            {
                _ = exception;
                options = null;
            }
        }
        else
        {
            // Настройку собрать не из чего (нет конструктора без параметров) —
            // отдаём заглушку интерфейса: у неё с DefaultValue.Mock свойство
            // Value вернёт мок, и код, читающий настройку, не упадёт на null.
            options = CreateMock(typeof(IOptions<>).MakeGenericType(valueType));
        }

        return options;
    }

    /// <summary>
    /// Заглушка создаётся рефлексией, потому что тип известен только в рантайме,
    /// а непустой класс Mock в Moq абстрактный.
    ///
    /// DefaultValue.Mock обязателен: у обычной loose-заглушки свойство
    /// интерфейсного типа возвращает null, и код, который это свойство читает,
    /// падал бы с NullReferenceException — то есть проверялась бы заглушка,
    /// а не сервис.
    /// </summary>
    public static object? CreateMock(Type type)
    {
        object? mock = null;

        try
        {
            var instance = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
            instance.DefaultValue = DefaultValue.Mock;
            mock = instance.Object;
        }
        catch (Exception exception)
            when (exception is ArgumentException or MemberAccessException or NotSupportedException)
        {
            mock = null;
        }

        return mock;
    }

    /// <summary>
    /// Instance у NullLogger&lt;T&gt; — статическое поле, а не свойство; иначе
    /// GetProperty вернул бы null и упал на null-разыменовании.
    /// </summary>
    private static object? ReadStatic(Type type, string name) =>
        type.GetProperty(name) is PropertyInfo property ? property.GetValue(null)
        : type.GetField(name) is FieldInfo field ? field.GetValue(null)
        : null;
}
