using System.Reflection;
using System.Text.RegularExpressions;
using Wolverine.Attributes;

namespace MARS.Shared.Messaging;

/// <summary>
/// Правила именования очередей и dead-letter очередей шины.
/// </summary>
public static class MarsQueues
{
    private const string ServicePrefix = "MARS.";
    private const string ListenerSuffix = "Listener";

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
    /// Формирует отдельную очередь для Listener на основании имени сервиса,
    /// identity его сообщения и имени самого Listener.
    /// </summary>
    public static string QueueNameForListener(Type listenerType, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(listenerType);

        var messageTypes = listenerType
            .GetInterfaces()
            .Where(
                interfaceType =>
                    interfaceType.IsGenericType
                    && interfaceType.GetGenericTypeDefinition() == typeof(IMarsListener<>)
            )
            .Select(interfaceType => interfaceType.GetGenericArguments()[0])
            .Distinct()
            .ToArray();

        if (messageTypes.Length != 1)
        {
            throw new InvalidOperationException(
                $"{listenerType.Name} must implement exactly one IMarsListener<TMessage>; "
                    + $"found {messageTypes.Length}."
            );
        }

        var messageType = messageTypes[0];
        var identity = messageType.GetCustomAttribute<MessageIdentityAttribute>();

        if (identity is null)
        {
            throw new InvalidOperationException(
                $"Missing MessageIdentity for {messageType.FullName ?? messageType.Name}."
            );
        }

        var service = NormalizeServiceName(serviceName);
        var message = NormalizeMessageAlias(identity.Alias);
        var listener = NormalizeListenerName(listenerType.Name);

        return $"mars.{service}.{message}.{listener}";
    }

    /// <summary>
    /// Возвращает общую DLQ сервиса. Сохраняет существующий формат
    /// <c>mars.{service}.dlq</c>, включая написание имени сервиса без kebab-case.
    /// </summary>
    public static string DeadLetterQueueForService(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var normalized = serviceName.Trim();

        if (normalized.StartsWith(ServicePrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[ServicePrefix.Length..];
        }

        return $"mars.{normalized.ToLowerInvariant()}.dlq";
    }

    private static string NormalizeServiceName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = name.Trim();

        if (normalized.StartsWith(ServicePrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[ServicePrefix.Length..];
        }

        return ToKebabCase(normalized);
    }

    private static string NormalizeMessageAlias(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        var normalized = alias.Trim();

        if (normalized.StartsWith("mars.", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["mars.".Length..];
        }

        return ToKebabCase(normalized);
    }

    private static string NormalizeListenerName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = name.EndsWith(ListenerSuffix, StringComparison.Ordinal)
            ? name[..^ListenerSuffix.Length]
            : name;

        return ToKebabCase(normalized);
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
                "A RabbitMQ name segment cannot be empty after normalization."
            );
        }

        return normalized;
    }
}
