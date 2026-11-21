using System.Reflection;

namespace MARS.Shared.Contracts;

/// <summary>
/// Рефлексивное описание веток <see cref="TelegramusEvent"/>.
/// </summary>
/// <remarks>
/// Единственное место, где решается, какие свойства сообщения являются ветками
/// события и какая ветка активна. Ветка — это читаемое и записываемое публичное
/// свойство; вычисляемые свойства (<c>EventCase</c>) в набор не попадают.
/// Порядок — порядок объявления, чтобы выбор при нескольких заполненных ветках
/// совпадал с прежним поведением ручной цепочки проверок.
/// </remarks>
public static class TelegramusEventContract
{
    private static readonly IReadOnlyList<PropertyInfo> BranchProperties = typeof(TelegramusEvent)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.CanRead && property.CanWrite)
        .OrderBy(property => property.MetadataToken)
        .ToArray();

    /// <summary>Все ветки события.</summary>
    public static IReadOnlyList<PropertyInfo> Branches => BranchProperties;

    /// <summary>
    /// Первая заполненная ветка: имя метода хаба и полезная нагрузка.
    /// </summary>
    public static (string Name, object? Payload)? GetActiveBranch(TelegramusEvent notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        foreach (var property in BranchProperties)
        {
            var payload = property.GetValue(notification);
            if (payload is not null)
            {
                return (property.Name, payload);
            }
        }

        return null;
    }
}
