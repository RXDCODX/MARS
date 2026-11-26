using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.TestKit;

public static class DependencyStubFactory
{
    public static ServiceProvider Build(IEnumerable<Type> roots, int depth = 2)
    {
        var services = new ServiceCollection();
        var registered = new HashSet<Type>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        foreach (var root in roots)
        {
            Register(services, root, registered, depth);
        }

        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildFor(Type root, int depth = 2) => Build([root], depth);

    public static void RegisterDependencies(
        IServiceCollection services,
        IEnumerable<Type> roots,
        int depth = 2
    )
    {
        var registered = new HashSet<Type>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        foreach (var root in roots)
        {
            foreach (
                var constructor in root.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            )
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    Register(services, parameter.ParameterType, registered, depth);
                }
            }
        }
    }

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
            // Options.Create(options) выводит IOptions<object>, когда options
            // имеет статический тип object. DI тогда не может привести
            // OptionsWrapper<object> к IOptions<TOptions> конкретного сервиса.
            result = Activator.CreateInstance(
                typeof(OptionsWrapper<>).MakeGenericType(type.GetGenericArguments()[0]),
                options
            )!;
        }
        else if (type.IsInterface || type.IsAbstract)
        {
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
