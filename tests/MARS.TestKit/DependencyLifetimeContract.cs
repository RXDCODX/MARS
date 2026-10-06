using System.Reflection;
using System.Text.RegularExpressions;

namespace MARS.TestKit;

/// <summary>
/// Lifetime-контракт сервиса: синглтон не должен держать scoped-зависимость, а
/// конструктор не должен требовать сервис, которого в контейнере нет.
/// </summary>
/// <remarks>
/// Ровно эти две ошибки валит контейнер в Development, где
/// <c>WebApplicationBuilder</c> включает <c>ValidateScopes</c> и
/// <c>ValidateOnBuild</c>. В Production обе проверки выключены по умолчанию,
/// поэтому дефект жил незамеченным: сервис запускался, а первое же разрешение
/// падало в рантайме — либо, что хуже, не падало никогда, потому что потребителя
/// никто не разрешал.
/// <para>
/// Проверка читает реальные регистрации из <c>Program.cs</c> и сопоставляет их с
/// конструкторами типов сборки, а не повторяет регистрации в тесте. Поэтому она
/// ловит регрессию в коде сервиса, тогда как контейнер с дублем регистраций
/// внутри теста всегда был бы зелёным.
/// </para>
/// <para>
/// Границы проверки намеренные: регистрации через фабрику
/// (<c>AddSingleton&lt;T&gt;(sp =&gt; …)</c>) не разбираются — из текста не видно
/// графа, и такой тип считается управляемым контейнером, а не проверяемым.
/// Регистрации из <c>MARS.Shared</c> не разбираются по той же причине: они в
/// другом файле.
/// </para>
/// </remarks>
public static partial class DependencyLifetimeContract
{
    /// <summary>Проверяет регистрации сервиса; пустой список — успех.</summary>
    /// <param name="serviceAssembly">Имя сборки сервиса, например <c>MARS.TwitchCore</c>.</param>
    /// <param name="programFile">
    /// Каталог с исходниками сервиса. По умолчанию берётся путь по соглашению
    /// репозитория — <c>src/{сборка}</c>; читаются все <c>*.cs</c>, а не только
    /// <c>Program.cs</c>, потому что регистрации лежат и в
    /// <c>*ServiceCollectionExtensions</c>, а проверка должна видеть их все.
    /// </param>
    public static IReadOnlyList<string> Verify(
        string serviceAssembly,
        string? sourceDirectory = null
    )
    {
        var directory = sourceDirectory ?? $"src/{serviceAssembly}";
        var registrations = ReadRegistrations(RepositoryFile.Sources(directory));
        var sources = $"{directory} (файлов: {registrations.Sources})";

        if (registrations.Count == 0)
        {
            return [$"{serviceAssembly}: в {sources} не найдено ни одной регистрации."];
        }

        var types = Assembly
            .Load(serviceAssembly)
            .GetTypes()
            .Where(type => type.IsClass || type.IsInterface)
            .ToArray();

        var failures = new List<string>();

        foreach (var registration in registrations.Values)
        {
            var implementation = Resolve(registration.Implementation, types);

            if (implementation is null)
            {
                continue;
            }

            failures.AddRange(
                UnregisteredDependencies(implementation, registration, registrations, types)
            );

            if (IsLongLived(registration))
            {
                failures.AddRange(
                    CaptiveDependencies(implementation, registration, registrations, types)
                );
            }
        }

        return failures;
    }

    /// <summary>Scoped-зависимость в синглтоне: она переживает свой scope.</summary>
    private static IEnumerable<string> CaptiveDependencies(
        Type consumer,
        Registration consumerRegistration,
        IReadOnlyDictionary<string, Registration> registrations,
        Type[] types
    )
    {
        foreach (var parameter in ConstructorParameters(consumer))
        {
            var registration = Find(registrations, parameter.ParameterType, types);

            if (registration?.Lifetime == Lifetime.Scoped)
            {
                yield return $"{consumer.FullName} ({consumerRegistration.Origin}) держит"
                    + $" scoped-зависимость {Describe(parameter.ParameterType)}"
                    + $" ({registration.Origin}). Синглтон переживает scope, и в"
                    + " Development такой граф роняет сборку контейнера"
                    + " (ValidateScopes), а в Production отдаёт captive-объект.";
            }
        }
    }

    /// <summary>
    /// Зависимость, которой в контейнере нет вовсе: разрешение упадёт в рантайме,
    /// причём не сразу, а на первом обращении.
    /// </summary>
    private static IEnumerable<string> UnregisteredDependencies(
        Type consumer,
        Registration consumerRegistration,
        IReadOnlyDictionary<string, Registration> registrations,
        Type[] types
    )
    {
        foreach (var parameter in ConstructorParameters(consumer))
        {
            if (
                Find(registrations, parameter.ParameterType, types) is null
                && OwnsType(parameter.ParameterType, consumer)
            )
            {
                yield return $"{consumer.FullName} ({consumerRegistration.Origin}) требует"
                    + $" {Describe(parameter.ParameterType)}, а зарегистрировать его"
                    + " негде: в контейнере такого сервиса нет, и разрешение упадёт"
                    + " в рантайме.";
            }
        }
    }

    /// <summary>
    /// Параметры конструктора, которым контейнер построит реализацию: при
    /// нескольких публичных берётся самый длинный — так же выбирает контейнер.
    /// </summary>
    private static IEnumerable<ParameterInfo> ConstructorParameters(Type consumer)
    {
        return consumer
                .GetConstructors()
                .Where(constructor =>
                    constructor.IsPublic && constructor.GetParameters().Length > 0
                )
                .OrderBy(constructor => constructor.GetParameters().Length)
                .Select(constructor => constructor.GetParameters())
                .FirstOrDefault()
            ?? [];
    }

    /// <summary>Регистрация параметра: по имени типа сервиса, затем по реализации.</summary>
    private static Registration? Find(
        IReadOnlyDictionary<string, Registration> registrations,
        Type parameterType,
        Type[] types
    )
    {
        if (registrations.TryGetValue(parameterType.Name, out var direct))
        {
            return direct;
        }

        return registrations.Values.FirstOrDefault(registration =>
            Resolve(registration.Implementation, types) == parameterType
        );
    }

    /// <summary>Тип объявлен в том же сервисе, а не в общей библиотеке.</summary>
    private static bool OwnsType(Type parameterType, Type consumer)
    {
        var service = consumer.Namespace?.Split('.')[0] ?? string.Empty;

        return parameterType.Namespace is not null
            && parameterType.Namespace.StartsWith(service + ".", StringComparison.Ordinal)
            && !parameterType.Namespace.StartsWith("MARS.Shared", StringComparison.Ordinal);
    }

    private static bool IsLongLived(Registration registration)
    {
        return registration.Lifetime is Lifetime.Singleton or Lifetime.Hosted;
    }

    private static Type? Resolve(string name, Type[] types)
    {
        return types.FirstOrDefault(type => type.Name == name);
    }

    private static string Describe(Type type)
    {
        return type.Name;
    }

    /// <summary>Читает регистрации сервиса из всех <c>*.cs</c> его каталога.</summary>
    private static Registrations ReadRegistrations(IEnumerable<string> sources)
    {
        var registrations = new Registrations();

        foreach (var source in sources)
        {
            registrations.Sources++;

            var text = File.ReadAllText(source);
            var file = Path.GetFileName(source);

            foreach (Match match in RegistrationPattern().Matches(text))
            {
                var method = match.Groups["method"].Value;
                var arguments = match.Groups["types"].Value;

                foreach (var (service, implementation) in Split(arguments))
                {
                    registrations.Add(
                        service,
                        new Registration(
                            service,
                            implementation,
                            LifetimeOf(method),
                            $"{method}<{arguments}> в {file}:{LineOf(text, match.Index)}"
                        )
                    );
                }
            }

            ReadFactories(registrations, text, file);
        }

        return registrations;
    }

    /// <summary>
    /// Регистрации через фабрику: <c>AddSingleton(sp =&gt; …)</c>. Тип, который
    /// она отдаёт, из вызова метода не виден — он виден внутри тела, и разбирать
    /// приходится его.
    /// </summary>
    /// <remarks>
    /// Без этого каждая такая регистрация выглядела бы как отсутствующая, и
    /// проверка ругалась бы на <c>MediaGitOptions</c> и <c>MediaStorageOptions</c>,
    /// которые сервис отдаёт как <c>IOptions&lt;T&gt;.Value</c>. Фабрика с
    /// <c>new X(…)</c> попутно называет реализацию, и она тоже проверяется.
    /// </remarks>
    private static void ReadFactories(Registrations registrations, string text, string file)
    {
        var lines = text.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var start = FactoryPattern().Match(lines[index]);

            if (!start.Success)
            {
                continue;
            }

            var method = start.Groups["method"].Value;
            var body = Body(lines, index, out var last);
            var origin = $"{method}(фабрика) в {file}:{index + 1}";

            foreach (Match provided in ProvidedPattern().Matches(body))
            {
                var type = provided.Groups["type"].Value;

                registrations.Add(type, new Registration(type, type, LifetimeOf(method), origin));
            }

            index = last;
        }
    }

    /// <summary>Тело вызова фабрики: от открытой скобки до закрытой, по нескольким строкам.</summary>
    private static string Body(IReadOnlyList<string> lines, int start, out int last)
    {
        var depth = 0;
        var body = new System.Text.StringBuilder();

        for (var index = start; index < lines.Count; index++)
        {
            body.AppendLine(lines[index]);

            foreach (var character in lines[index])
            {
                if (character == '(')
                {
                    depth++;
                }
                else if (character == ')')
                {
                    depth--;
                }
            }

            if (depth <= 0)
            {
                last = index;

                return body.ToString();
            }
        }

        last = lines.Count - 1;

        return body.ToString();
    }

    /// <summary>
    /// Разбирает список типов регистрации: <c>T</c> либо <c>TService, TImplementation</c>.
    /// </summary>
    private static IEnumerable<(string Service, string Implementation)> Split(string arguments)
    {
        var names = arguments
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Split('<')[0].Trim())
            .Where(name => name.Length > 0)
            .ToArray();

        if (names.Length == 0)
        {
            yield break;
        }

        yield return (names[0], names.Length > 1 ? names[1] : names[0]);
    }

    private static Lifetime LifetimeOf(string method)
    {
        if (method.StartsWith("AddScoped", StringComparison.Ordinal))
        {
            return Lifetime.Scoped;
        }

        if (
            method.StartsWith("AddTransient", StringComparison.Ordinal)
            || method.StartsWith("AddHttpClient", StringComparison.Ordinal)
        )
        {
            return Lifetime.Transient;
        }

        return method.StartsWith("AddHostedService", StringComparison.Ordinal)
            ? Lifetime.Hosted
            : Lifetime.Singleton;
    }

    private static int LineOf(string source, int index)
    {
        return source[..index].Count(character => character == '\n') + 1;
    }

    /// <summary>Методы регистрации, чей список типов читается из угловых скобок.</summary>
    [GeneratedRegex(
        @"(?:builder\.)?services\s*\.\s*(?<method>(?:TryAdd|Add)(?:Singleton|Scoped|Transient|HostedService|HttpClient))\s*<\s*(?<types>[^>]+)>",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex RegistrationPattern();

    /// <summary>Начало регистрации, за которой стоит тело вызова: <c>AddSingleton(…)</c>.</summary>
    [GeneratedRegex(
        @"(?:builder\.)?services\s*\.\s*(?<method>(?:TryAdd|Add)(?:Singleton|Scoped|Transient))(?:\s*<[^>]*>)?\s*\(",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex FactoryPattern();

    /// <summary>Тип, который отдаёт фабрика: <c>new X(…)</c> либо <c>IOptions&lt;X&gt;</c>.</summary>
    [GeneratedRegex(
        @"new\s+(?<type>[A-Z]\w+)\s*[(<]|IOptions\s*<\s*(?<type>\w+)\s*>",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex ProvidedPattern();

    private sealed record Registration(
        string Service,
        string Implementation,
        Lifetime Lifetime,
        string Origin
    );

    /// <summary>
    /// Регистрации, собранные по всем файлам сервиса, с числом прочитанных
    /// файлов: ноль файлов означал бы, что каталог не найден, и проверка прошла бы
    /// вхолостую.
    /// </summary>
    private sealed class Registrations : IReadOnlyDictionary<string, Registration>
    {
        private readonly Dictionary<string, Registration> _byService = new(StringComparer.Ordinal);

        public int Sources { get; set; }

        public int Count => _byService.Count;

        public Registration this[string key] => _byService[key];

        public IEnumerable<string> Keys => _byService.Keys;

        public IEnumerable<Registration> Values => _byService.Values;

        public void Add(string service, Registration registration)
        {
            _byService[service] = registration;
        }

        public bool ContainsKey(string key)
        {
            return _byService.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, Registration>> GetEnumerator()
        {
            return _byService.GetEnumerator();
        }

        public bool TryGetValue(string key, out Registration value)
        {
            return _byService.TryGetValue(key, out value!);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private enum Lifetime
    {
        Singleton,
        Scoped,
        Transient,
        Hosted,
    }
}
