using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MARS.Platform.Infrastructure.Hosting.Swagger;

/// <summary>
/// Назначает <c>schemaId</c> по короткому имени типа, а при коллизии (несколько
/// типов с одинаковым коротким именем, например <c>OperationResult</c> в
/// <c>MARS.Shared.Kernel</c> и <c>MARS.Discord.Application.Models</c>) — по
/// полному имени с заменой <c>.</c> и <c>+</c> на <c>_</c>.
/// </summary>
/// <remarks>
/// <para>
/// Короткое имя сохраняется для всех неконфликтующих типов, поэтому
/// существующие снапшоты (<c>swagger_*.json</c>) и сгенерированные клиенты не
/// меняют имена.
/// </para>
/// <para>
/// Владелец короткого имени фиксируется первым типом, который его запросил:
/// при повторных выборках документа (кэширование <c>SwaggerGenOptions</c>) имя
/// не «флип-флопит» — первый владелец удерживает его, а конфликтующие типы
/// стабильно получают полное имя.
/// </para>
/// </remarks>
public static class MarsSchemaIdExtensions
{
    public static void UseCollisionAwareSchemaIds(this SwaggerGenOptions options)
    {
        var owners = new Dictionary<string, Type>(StringComparer.Ordinal);

        options.CustomSchemaIds(type =>
        {
            var name = type.Name;

            if (owners.TryGetValue(name, out var owner) && !ReferenceEquals(owner, type))
            {
                return (type.FullName ?? type.Name).Replace(".", "_").Replace("+", "_");
            }

            owners[name] = type;
            return name;
        });
    }
}
