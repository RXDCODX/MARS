using System.Reflection;
using System.Runtime.CompilerServices;

namespace MARS.TestKit;

/// <summary>
/// Проверка контракта публичной поверхности сборки сервиса: каждый публичный тип
/// создаётся из подставляемых значений, каждое читаемое и записываемое свойство
/// возвращает положенное значение обратно, а статические конструкторы всех типов
/// отрабатывают без исключений.
///
/// Зачем это нужно при пороге по методам: у моделей, DTO и конфигураций основная
/// масса методов — это конструкторы и свойства. Проверять их по одному вручную
/// бессмысленно (сотни одинаковых тестов), а не проверять нельзя: именно на них
/// приходится треть знаменателя покрытия.
///
/// Что проверка НЕ делает: не вызывает методы с побочными эффектами и не
/// собирает сервисы с настоящими зависимостями. Это делают точечные тесты.
/// </summary>
public static class PublicContractVerifier
{
    /// <summary>
    /// Прогоняет проверку по сборке. Исключение наружу не пробрасывается: каждое
    /// нарушение попадает в <see cref="ContractVerificationResult.Violations"/>,
    /// иначе первое же упавшее свойство скрыло бы все остальные.
    /// </summary>
    public static ContractVerificationResult Verify(
        Assembly assembly,
        ContractOptions? options = null
    )
    {
        var settings = options ?? new ContractOptions();
        var result = new ContractVerificationResult();

        if (settings.RunStaticInitializers)
        {
            foreach (var type in EnumerateAllTypes(assembly))
            {
                RunStaticInitializer(type, result);
            }
        }

        foreach (var type in EnumerateContractTypes(assembly, settings))
        {
            VerifyType(type, assembly, result, settings);
        }

        return result;
    }

    /// <summary>
    /// То же, что <see cref="Verify(Assembly, ContractOptions?)"/>, но по имени
    /// сборки. Имя нужно, чтобы тест не ссылался на тип сервиса: у служебных
    /// типов нет естественного «якоря», а пустая ссылка на сборку видна только
    /// по срыву загрузки.
    /// </summary>
    public static ContractVerificationResult VerifyAssembly(
        string assemblyName,
        ContractOptions? options = null
    )
    {
        var result = Verify(Assembly.Load(assemblyName), options);
        return result;
    }

    /// <summary>
    /// Раскладывает итог проверки в строки для сообщения об ошибке. Номера
    /// строк совпадают с выводом <c>dotnet test</c>, поэтому список можно
    /// читать прямо из окна терминала.
    /// </summary>
    public static string Describe(ContractVerificationResult result)
    {
        var lines = new List<string>
        {
            $"Проверено типов: {result.TypesInspected}, создано: {result.TypesConstructed}, "
                + $"конструкторов: {result.ConstructorsInvoked}, свойств: {result.PropertiesChecked}, "
                + $"статических инициализаторов: {result.StaticInitializersRun}.",
            $"Пропущено типов: {result.SkippedTypes.Count}.",
        };

        foreach (var violation in result.Violations)
        {
            lines.Add($"  {violation}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<Type> EnumerateAllTypes(Assembly assembly)
    {
        var result = new List<Type>();

        try
        {
            result.AddRange(assembly.GetTypes());
        }
        catch (ReflectionTypeLoadException exception)
        {
            // Часть типов могла не загрузиться — их проверяем отдельно, когда
            // починится причина, но прогон не падает целиком.
            result.AddRange(exception.Types.Where(type => type is not null)!);
        }

        return result;
    }

    private static IEnumerable<Type> EnumerateContractTypes(
        Assembly assembly,
        ContractOptions settings
    )
    {
        var result = new List<Type>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (
                IsContractCandidate(type, assembly)
                && !settings.IgnoredTypes.Contains(type.FullName ?? type.Name)
            )
            {
                result.Add(type);
            }
        }

        return result;
    }

    /// <summary>
    /// Отбирает типы, которые безопасно создавать: собственные, не компиляторные
    /// и не наследованные от системных типов.
    ///
    /// Про наследование отдельно. Подтип FileStream, StreamReader или HttpClient
    /// создаётся «с данными» — а это открытие файла или сокета прямо в
    /// проверке, то есть побочный эффект вместо теста. То же с клиентами,
    /// которые в конструкторе идут в сеть: их создание проверяет сеть, а не
    /// контракт, и может вообще не вернуться.
    /// </summary>
    private static bool IsContractCandidate(Type type, Assembly assembly)
    {
        var result = true;

        if (type.IsInterface || type.IsAbstract || type.IsEnum || type.IsGenericTypeDefinition)
        {
            result = false;
        }
        else if (typeof(Delegate).IsAssignableFrom(type))
        {
            result = false;
        }
        else if (type.Name.StartsWith('<'))
        {
            // Компиляторные типы: анонимные, итераторы, замыкания, обвязки
            // async-методов. Их создаёт не код сервиса, а компилятор.
            result = false;
        }
        else if (type.IsNested)
        {
            // Вложенные публичные и внутренние классы проверяются вместе с
            // внешним: они тоже часть контракта.
            result = !type.IsNestedPrivate;
        }
        else
        {
            result =
                type.BaseType is null
                || type.BaseType == typeof(object)
                || type.BaseType.Assembly == assembly;
        }

        return result;
    }

    private static void RunStaticInitializer(Type type, ContractVerificationResult result)
    {
        if (
            type.IsGenericTypeDefinition
            || type.Name.StartsWith('<')
            || type.ContainsGenericParameters
        )
        {
            return;
        }

        result.CountStaticInitializer();

        try
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }
        catch (Exception exception)
        {
            result.AddViolation(
                ContractViolationKind.StaticInitializerThrew,
                type,
                null,
                Describe(exception)
            );
        }
    }

    private static void VerifyType(
        Type type,
        Assembly assembly,
        ContractVerificationResult result,
        ContractOptions settings
    )
    {
        result.CountType();

        var instance = TryCreateInstance(type, result, settings);
        if (instance is null)
        {
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.DeclaringType?.Assembly != assembly)
            {
                // Свойства базовых классов из BCL (Position у FileStream,
                // Timeout у Stream) принадлежат не сервису: они и не должны
                // вести себя как «поле DTO».
                continue;
            }
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            if (property.GetMethod is null || property.SetMethod is null)
            {
                continue;
            }
            if (property.GetMethod.IsStatic || property.SetMethod.IsStatic)
            {
                continue;
            }

            VerifyProperty(type, instance, property, result, settings);
        }
    }

    private static object? TryCreateInstance(
        Type type,
        ContractVerificationResult result,
        ContractOptions settings
    )
    {
        object? instance = null;

        if (HasUnsamplableParameter(type, settings))
        {
            // Сервис, а не данные: в конструкторе у него интерфейсы
            // (ILogger, IHttpClientFactory, контекст БД), и подставлять null
            // бессмысленно — конструктор всё равно упадёт, а настоящие
            // зависимости подставляет точечный тест.
            result.SkipType(type, "в конструкторе есть зависимости — собирается точечным тестом");
        }
        else if (type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
        {
            // Тип производится фабриками: у него нет публичного конструктора
            // (например, private у record-результата). Сами фабрики проверяются
            // точечными тестами — у них есть вход и выход, который видно.
            result.SkipType(type, "нет публичного конструктора — собирается фабриками");
        }
        else if (type.GetConstructor(Type.EmptyTypes) is not null)
        {
            instance = Create(type, result, settings);
        }
        else
        {
            var created = CreateFromWidestConstructor(type, result, settings);
            if (created.Instance is not null)
            {
                instance = created.Instance;
            }
            else if (!created.Attempted)
            {
                // NotConstructible — только когда ни один конструктор даже не
                // пробовали вызвать. Упавший конструктор уже записан как
                // ConstructorThrew, и второй диагноз на тот же тип только
                // запутывает.
                result.AddViolation(
                    ContractViolationKind.NotConstructible,
                    type,
                    null,
                    "Нет конструктора, все аргументы которого можно подставить."
                );
            }
        }

        return instance;
    }

    private static bool HasUnsamplableParameter(Type type, ContractOptions settings) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter =>
                SampleValue.TryCreate(parameter.ParameterType, settings.MaxDepth - 1) is null
            );

    /// <summary>
    /// Пробует конструкторы от самого короткого списка аргументов к самому
    /// длинному: короткий обычно и есть «конструктор данных», длинные требуют
    /// зависимостей сервиса.
    /// </summary>
    private static (object? Instance, bool Attempted) CreateFromWidestConstructor(
        Type type,
        ContractVerificationResult result,
        ContractOptions settings
    )
    {
        object? instance = null;
        var attempted = false;

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(candidate => candidate.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var arguments = BuildArguments(constructor, settings.MaxDepth);
            if (arguments is null)
            {
                continue;
            }

            attempted = true;
            var created = Invoke(type, constructor, arguments, result, settings);
            if (created is not null)
            {
                instance = created;
                break;
            }
        }

        return (instance, attempted);
    }

    private static object?[]? BuildArguments(ConstructorInfo constructor, int depth)
    {
        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var index = 0; index < parameters.Length; index++)
        {
            var value = SampleValue.TryCreate(parameters[index].ParameterType, depth - 1);
            if (value is null)
            {
                // null не подставляем ни для ссылочных типов, ни для значимых:
                // конструктор, который разыменовывает зависимость, упал бы с
                // NullReferenceException, и падение выглядело бы как ошибка
                // сервиса, а не как «этот тип проверяет точечный тест».
                return null;
            }
            arguments[index] = value;
        }

        return arguments;
    }

    private static object? Create(
        Type type,
        ContractVerificationResult result,
        ContractOptions settings
    )
    {
        var constructor = type.GetConstructor(Type.EmptyTypes)!;
        return Invoke(type, constructor, [], result, settings);
    }

    private static object? Invoke(
        Type type,
        ConstructorInfo constructor,
        object?[] arguments,
        ContractVerificationResult result,
        ContractOptions settings
    )
    {
        object? instance = null;

        try
        {
            instance = constructor.Invoke(arguments);
            result.CountConstructor();
            result.CountConstructed();
        }
        catch (Exception exception)
        {
            result.AddViolation(
                ContractViolationKind.ConstructorThrew,
                type,
                null,
                Describe(exception)
            );
        }

        return instance;
    }

    private static void VerifyProperty(
        Type type,
        object instance,
        PropertyInfo property,
        ContractVerificationResult result,
        ContractOptions settings
    )
    {
        var candidates = SampleValue.TryCreateCandidates(property.PropertyType, settings.MaxDepth);
        if (candidates.Count == 0)
        {
            result.SkipType(
                property.PropertyType,
                $"свойство {type.Name}.{property.Name} пропущено: нет подставляемого значения"
            );
            return;
        }

        result.CountProperty();

        var roundTripped = false;
        var lastFailure = "значение принято, но прочитано не то же";

        foreach (var candidate in candidates)
        {
            var outcome = TryRoundTrip(property, instance, candidate);
            if (outcome is not null)
            {
                lastFailure = outcome;
                continue;
            }

            roundTripped = true;
            break;
        }

        if (!roundTripped)
        {
            result.AddViolation(
                ContractViolationKind.PropertyRoundTripFailed,
                type,
                property.Name,
                lastFailure
            );
        }
    }

    /// <summary>
    /// Один круг «положить — прочитать». null означает успех, строку — причину,
    /// почему значение не подошло.
    /// </summary>
    private static string? TryRoundTrip(PropertyInfo property, object instance, object candidate)
    {
        string? failure = null;

        try
        {
            property.SetValue(instance, candidate);
            var stored = property.GetValue(instance);
            if (!SampleValue.Matches(candidate, stored))
            {
                failure = $"положили {DescribeValue(candidate)}, получили {DescribeValue(stored)}";
            }
        }
        catch (Exception exception)
        {
            failure = Describe(exception);
        }

        return failure;
    }

    private static string Describe(Exception exception) =>
        exception is TargetInvocationException { InnerException: { } inner }
            ? $"{inner.GetType().Name}: {inner.Message}"
            : $"{exception.GetType().Name}: {exception.Message}";

    private static string DescribeValue(object? value) =>
        value switch
        {
            null => "null",
            string text => $"\"{text}\"",
            _ => $"{value.GetType().Name} {value}",
        };
}
