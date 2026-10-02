using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.TestKit;

/// <summary>
/// Собирает <see cref="ServiceProvider"/>, в котором любой интерфейс отдаётся
/// loose-заглушкой Moq, а любой класс создаётся через <see cref="ActivatorUtilities"/>.
///
/// Нужно там, где сервис сам ищет зависимости рефлексией по типам команд или
/// воркеров: настоящий контейнер с настоящими клиентами уехал бы в сеть,
/// пустой — не дал бы собрать ничего, а перечислять зависимости каждого
/// сервиса руками пришлось бы дважды (в тесте и в коде).
///
/// Проверяет это сборка, а не поведение: «сервис собирается» — единственное,
/// что можно утверждать про заглушки.
/// </summary>
public static class DependencyStubFactory
{
    /// <summary>
    /// Контейнер, в котором разрешаются зависимости указанных типов и всего,
    /// что нужно их конструкторам, на глубине <paramref name="depth"/>.
    /// </summary>
    public static ServiceProvider Build(IEnumerable<Type> roots, int depth = 2)
    {
        var services = new ServiceCollection();
        var registered = new HashSet<Type>();

        foreach (var root in roots)
        {
            Register(services, root, registered, depth);
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Собирает контейнер с зависимостями одного типа. Корневой тип сам не
    /// регистрируется — его создаёт вызывающий, чтобы мог подставить свои
    /// настоящие зависимости вместо заглушек.
    /// </summary>
    public static ServiceProvider BuildFor(Type root, int depth = 2) => Build([root], depth);

    private static void Register(
        IServiceCollection services,
        Type type,
        HashSet<Type> registered,
        int depth
    )
    {
        if (depth < 0 || !registered.Add(type))
        {
            return;
        }
        if (type.IsPrimitive || type == typeof(string) || type == typeof(CancellationToken))
        {
            return;
        }

        services.AddSingleton(type, provider => Create(type, provider));

        foreach (
            var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
        )
        {
            foreach (var parameter in constructor.GetParameters())
            {
                Register(services, parameter.ParameterType, registered, depth - 1);
            }
        }
    }

    private static object Create(Type type, IServiceProvider provider)
    {
        object? result = null;

        if (type == typeof(ILogger))
        {
            result = NullLogger.Instance;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            result = ActivatorUtilities.CreateInstance(
                provider,
                typeof(NullLogger<>).MakeGenericType(type.GetGenericArguments())
            );
        }
        else if (
            type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(IOptions<>)
            && SampleValue.TryCreate(type.GetGenericArguments()[0], 2) is { } options
        )
        {
            result = Options.Create(options);
        }
        else if (type.IsInterface || type.IsAbstract)
        {
            // DefaultValue.Mock: у обычной loose-заглушки свойство интерфейсного
            // типа возвращает null, и код, который это свойство читает, падал
            // бы с NullReferenceException — то есть проверялась бы заглушка, а
            // не сервис.
            var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
            mock.DefaultValue = DefaultValue.Mock;
            result = mock.Object;
        }
        else
        {
            result = ActivatorUtilities.CreateInstance(provider, type);
        }

        return result;
    }
}
