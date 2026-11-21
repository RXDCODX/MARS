using System.Collections.Concurrent;
using System.Reflection;

namespace MARS.Shared.Contracts;

/// <summary>
/// Общая рефлексивная модель сообщения-«oneof»: набор веток и выбор активной.
/// </summary>
/// <remarks>
/// Ветка — публичное свойство с геттером и сеттером. Вычисляемые свойства
/// (<c>EventCase</c>) в набор не попадают. Порядок — порядок объявления:
/// при нескольких заполненных ветках выбирается первая, как и в прежних
/// ручных цепочках проверок.
/// </remarks>
public static class OneofEventContract
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyInfo>> BranchesByType =
        new();

    public static IReadOnlyList<PropertyInfo> Branches(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return BranchesByType.GetOrAdd(
            messageType,
            type =>
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanRead && property.CanWrite)
                    .OrderBy(property => property.MetadataToken)
                    .ToArray()
        );
    }

    public static (string Name, object? Payload)? GetActiveBranch(object message)
    {
        ArgumentNullException.ThrowIfNull(message);

        foreach (var property in Branches(message.GetType()))
        {
            var payload = property.GetValue(message);
            if (payload is not null)
            {
                return (property.Name, payload);
            }
        }

        return null;
    }
}
