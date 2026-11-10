using System.Text.RegularExpressions;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Единое правило формирования имени RabbitMQ exchange из типа сообщения.
/// Имя типа является частью интеграционного контракта: изменение имени класса
/// меняет имя exchange.
/// </summary>
public static class MarsMessageNaming
{
    private const string EventSuffix = "Event";

    private static readonly Regex LowercaseToUppercaseBoundary = new(
        "([a-z0-9])([A-Z])",
        RegexOptions.CultureInvariant
    );

    private static readonly Regex AcronymBoundary = new(
        "([A-Z])([A-Z][a-z])",
        RegexOptions.CultureInvariant
    );

    private static readonly Regex NonAlphaNumeric = new(
        "[^A-Za-z0-9]+",
        RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Возвращает имя exchange в формате <c>mars.{message-name}</c>.
    /// Суффикс <c>Event</c> в имени CLR-типа не дублируется в имени exchange.
    /// </summary>
    public static string ExchangeNameFor(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (!typeof(IMarsEventMessage).IsAssignableFrom(messageType))
        {
            throw new InvalidOperationException(
                $"{messageType.FullName ?? messageType.Name} must implement IMarsEventMessage."
            );
        }

        if (!messageType.IsClass || messageType.IsAbstract || messageType.ContainsGenericParameters)
        {
            throw new InvalidOperationException(
                $"{messageType.FullName ?? messageType.Name} must be a concrete, closed message class."
            );
        }

        var name = messageType.Name;

        if (name.EndsWith(EventSuffix, StringComparison.Ordinal))
        {
            name = name[..^EventSuffix.Length];
        }

        return $"mars.{ToKebabCase(name)}";
    }

    private static string ToKebabCase(string value)
    {
        var normalized = LowercaseToUppercaseBoundary.Replace(value, "$1-$2");
        normalized = AcronymBoundary.Replace(normalized, "$1-$2");
        normalized = NonAlphaNumeric.Replace(normalized, "-");
        normalized = normalized.Trim('-').ToLowerInvariant();

        if (normalized.Length == 0)
        {
            throw new InvalidOperationException(
                "A message type name cannot be empty after removing the Event suffix."
            );
        }

        return normalized;
    }
}
