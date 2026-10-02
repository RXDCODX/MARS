using System.Collections;
using System.Globalization;
using System.Reflection;

namespace MARS.TestKit;

/// <summary>
/// Подбирает значение, которое можно положить в свойство и получить обратно, и
/// создаёт объекты для конструкторов, которые не имеют версии без параметров
/// (например, record с позиционными параметрами).
///
/// Возвращает null, когда тип трогать нельзя: интерфейсы без реализации в
/// сборке, абстрактные классы, делегаты, указатели и потоки. Пропуск лучше
/// выдуманного значения: выдуманное значение молча сломало бы сравнение.
/// </summary>
public static class SampleValue
{
    private static readonly string[] Text = ["mars-contract-sample", "value.txt", "a.b.c.d", "42"];

    private static readonly DateTime Timestamp = new(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// Создаёт значение типа <paramref name="type"/> либо null, если тип не
    /// поддерживается. Глубина вложенности ограничена <paramref name="depth"/>:
    /// за её пределами типы просто пропускаются, рекурсия по графу без предела
    /// не заканчивается.
    /// </summary>
    public static object? TryCreate(Type type, int depth)
    {
        var result = TryCreateCore(type, depth);
        return result;
    }

    /// <summary>
    /// Варианты значения по убыванию приоритета. У строк их несколько, потому
    /// что сеттеры бывают проверяющими: имя файла без точки расширения
    /// отвергается, а «value.txt» проходит. Первое подходящее значение и есть
    /// то, ради которого проверка нужна; если не подошло ни одно — свойство
    /// считается нарушением.
    /// </summary>
    public static IReadOnlyList<object> TryCreateCandidates(Type type, int depth)
    {
        var result = new List<object>();

        if (type == typeof(string))
        {
            result.AddRange(Text);
        }
        else
        {
            var single = TryCreateCore(type, depth);
            if (single is not null)
            {
                result.Add(single);
            }
        }

        return result;
    }

    /// <summary>
    /// Сравнивает ожидаемое и прочитанное обратно значения. Последовательности
    /// сравниваются по элементам, остальное — по Equals, потому что
    /// <c>Assert.Equal</c> в TestKit недоступен: проект не тянет xunit.
    /// </summary>
    public static bool Matches(object? expected, object? actual)
    {
        var result = false;

        if (expected is null || actual is null)
        {
            result = ReferenceEquals(expected, actual);
        }
        else if (expected is string || actual is string)
        {
            result = expected.Equals(actual);
        }
        else if (expected is IEnumerable expectedItems && actual is IEnumerable actualItems)
        {
            result = SequenceMatches(expectedItems, actualItems);
        }
        else
        {
            result = expected.Equals(actual);
        }

        return result;
    }

    private static object? TryCreateCore(Type type, int depth)
    {
        if (type.IsByRef || type.IsPointer)
        {
            return null;
        }

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            return TryCreateCore(underlying, depth);
        }

        var simple = TryCreateSimple(type);
        if (simple is not null)
        {
            return simple;
        }

        var collection = TryCreateCollection(type, depth);
        if (collection is not null)
        {
            return collection;
        }

        return depth > 0 ? TryCreateObject(type, depth) : null;
    }

    private static object? TryCreateSimple(Type type)
    {
        var result = (object?)null;

        if (type == typeof(string))
        {
            return Text[0];
        }
        else if (type == typeof(bool))
        {
            result = true;
        }
        else if (type == typeof(char))
        {
            result = 'm';
        }
        else if (type.IsEnum)
        {
            result = Enum.GetValues(type).GetValue(0);
        }
        else if (type == typeof(Guid))
        {
            result = Guid.Parse("11111111-2222-3333-4444-555555555555");
        }
        else if (type == typeof(DateTime))
        {
            result = Timestamp;
        }
        else if (type == typeof(DateTimeOffset))
        {
            result = new DateTimeOffset(Timestamp);
        }
        else if (type == typeof(TimeSpan))
        {
            result = TimeSpan.FromMinutes(7);
        }
        else if (type == typeof(Uri))
        {
            result = new Uri("https://mars.example.org/contract");
        }
        else if (type == typeof(Version))
        {
            result = new Version(1, 2);
        }
        else if (type == typeof(CancellationToken))
        {
            result = CancellationToken.None;
        }
        else if (IsNumeric(type))
        {
            var value =
                type == typeof(float) || type == typeof(double) || type == typeof(decimal)
                    ? 1.5m
                    : 7m;
            result = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        return result;
    }

    private static bool IsNumeric(Type type) =>
        type == typeof(byte)
        || type == typeof(sbyte)
        || type == typeof(short)
        || type == typeof(ushort)
        || type == typeof(int)
        || type == typeof(uint)
        || type == typeof(long)
        || type == typeof(ulong);

    private static object? TryCreateCollection(Type type, int depth)
    {
        if (type == typeof(string) || (type.IsArray && type.GetArrayRank() != 1))
        {
            return null;
        }

        var element = type.IsArray ? type.GetElementType() : FindItemType(type);
        if (element is null)
        {
            return null;
        }

        if (FindDictionaryType(type) is Type dictionaryType)
        {
            return Activator.CreateInstance(dictionaryType);
        }

        return type.IsArray ? CreateArray(element, depth) : CreateList(type, element, depth);
    }

    private static object CreateArray(Type element, int depth)
    {
        var item = depth > 0 ? TryCreateCore(element, depth - 1) : null;
        if (item is null)
        {
            return Array.CreateInstance(element, 0);
        }

        var array = Array.CreateInstance(element, 1);
        array.SetValue(item, 0);
        return array;
    }

    private static object? CreateList(Type type, Type element, int depth)
    {
        var listType = typeof(List<>).MakeGenericType(element);
        if (!type.IsAssignableFrom(listType))
        {
            return null;
        }

        var list = (IList)Activator.CreateInstance(listType)!;
        var item = depth > 0 ? TryCreateCore(element, depth - 1) : null;
        if (item is not null)
        {
            list.Add(item);
        }

        return list;
    }

    private static Type? FindItemType(Type type)
    {
        var result = (Type?)null;

        foreach (var candidate in type.GetInterfaces().Append(type))
        {
            if (!candidate.IsGenericType)
            {
                continue;
            }

            var definition = candidate.GetGenericTypeDefinition();
            var isSequence =
                definition == typeof(IEnumerable<>)
                || definition == typeof(IReadOnlyList<>)
                || definition == typeof(IReadOnlyCollection<>)
                || definition == typeof(ICollection<>);
            if (!isSequence)
            {
                continue;
            }

            var element = candidate.GetGenericArguments()[0];
            if (!IsKeyValuePair(element))
            {
                result = element;
            }
            break;
        }

        return result;
    }

    private static Type? FindDictionaryType(Type type) =>
        type.GetInterfaces()
            .Append(type)
            .FirstOrDefault(candidate =>
                candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>)
            );

    private static bool IsKeyValuePair(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

    /// <summary>
    /// Создаёт объект типа «данные»: у него есть публичный конструктор, все
    /// аргументы которого подставляются, и нет обязательных зависимостей.
    /// Сервисы с интерфейсами в конструкторе сюда не попадают — их собирают
    /// точечные тесты, где зависимости настоящие.
    /// </summary>
    private static object? TryCreateObject(Type type, int depth)
    {
        if (type.IsInterface || type.IsAbstract || typeof(Delegate).IsAssignableFrom(type))
        {
            return null;
        }
        if (type.IsGenericTypeDefinition || type.GetConstructors().Length == 0)
        {
            return null;
        }
        if (FindItemType(type) is not null || FindDictionaryType(type) is not null)
        {
            return null;
        }

        var candidates = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(candidate => candidate.GetParameters().Length)
            .ToArray();

        foreach (var constructor in candidates)
        {
            var arguments = BuildArguments(constructor, depth);
            if (arguments is not null)
            {
                return constructor.Invoke(arguments);
            }
        }

        return null;
    }

    private static object?[]? BuildArguments(ConstructorInfo constructor, int depth)
    {
        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var index = 0; index < parameters.Length; index++)
        {
            var value = TryCreateCore(parameters[index].ParameterType, depth - 1);
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

    private static bool SequenceMatches(IEnumerable expected, IEnumerable actual)
    {
        var result = true;
        var expectedEnumerator = expected.GetEnumerator();
        var actualEnumerator = actual.GetEnumerator();

        while (true)
        {
            var expectedMoved = expectedEnumerator.MoveNext();
            var actualMoved = actualEnumerator.MoveNext();

            if (expectedMoved != actualMoved)
            {
                result = false;
                break;
            }
            if (!expectedMoved)
            {
                break;
            }
            if (!Matches(expectedEnumerator.Current, actualEnumerator.Current))
            {
                result = false;
                break;
            }
        }

        return result;
    }
}
