using System.Reflection;

namespace MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

/// <summary>
/// Логический ключ текущего сервиса. Он приходит из AddMarsDefaults, потому что
/// адреса host/container и namespace проекта ненадёжны (например TwitchCore).
/// </summary>
public sealed record ServiceIdentity(string LogicalName, Assembly? EntryAssembly = null)
    : IServiceIdentity
{
    public string NamespaceServiceName { get; } = ResolveNamespaceServiceName(EntryAssembly);

    public string CanonicalName { get; } =
        ServiceNameRegistry.Canonicalize(
            ResolveNamespaceServiceName(EntryAssembly) is { Length: > 0 } namespaceName
                ? namespaceName
                : LogicalName
        );

    public bool IsSelf(string serviceKey) =>
        string.Equals(
            CanonicalName,
            ServiceNameRegistry.Canonicalize(serviceKey),
            StringComparison.Ordinal
        );

    private static string ResolveNamespaceServiceName(Assembly? assembly)
    {
        var name = assembly?.GetName().Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var segments = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? $"{segments[0]}.{segments[1]}" : name;
    }
}
