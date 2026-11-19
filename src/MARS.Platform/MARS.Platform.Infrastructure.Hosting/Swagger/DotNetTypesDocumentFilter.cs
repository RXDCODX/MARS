using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MARS.Platform.Infrastructure.Hosting.Swagger;

/// <summary>
/// Убирает из документа типы .NET, которые попадают в схему случайно:
/// рефлексию, хендлы, исключения и т.п.
/// </summary>
/// <remarks>
/// Часть моделей хабов содержит свойства инфраструктурного характера
/// (<c>MethodInfo</c>, <c>Exception</c>), и без фильтра они уезжают в
/// сгенерированный клиент как полноценные схемы.
/// </remarks>
public sealed class DotNetTypesDocumentFilter : IDocumentFilter
{
    private static readonly HashSet<string> BlockedSchemaNames = new(StringComparer.Ordinal)
    {
        // Core reflection and system types that should not appear in public schema
        nameof(Exception),
        nameof(MethodBase),
        nameof(MethodInfo),
        nameof(ConstructorInfo),
        nameof(MemberInfo),
        nameof(PropertyInfo),
        nameof(FieldInfo),
        nameof(EventInfo),
        nameof(ParameterInfo),
        nameof(Type),
        nameof(TypeInfo),
        nameof(Assembly),
        nameof(Module),
        nameof(ModuleHandle),
        nameof(RuntimeMethodHandle),
        nameof(RuntimeFieldHandle),
        nameof(RuntimeTypeHandle),
        nameof(IntPtr),
        nameof(ICustomAttributeProvider),
        nameof(CustomAttributeData),
        nameof(CustomAttributeNamedArgument),
        nameof(CustomAttributeTypedArgument),
        nameof(StructLayoutAttribute),
        // Frequently leaked framework models
        nameof(Color),
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Paths is not null)
        {
            foreach (var path in swaggerDoc.Paths.Values)
            {
                if (path.Operations is null)
                {
                    continue;
                }

                foreach (var operation in path.Operations.Values)
                {
                    if (operation.Parameters is { Count: > 0 })
                    {
                        operation.Parameters =
                        [
                            .. operation.Parameters.Where(p => !IsRefToBlocked(p.Schema)),
                        ];
                    }

                    if (operation.RequestBody?.Content is not null)
                    {
                        foreach (var mediaType in operation.RequestBody.Content.Values)
                        {
                            if (IsRefToBlocked(mediaType.Schema))
                            {
                                mediaType.Schema = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                };
                            }
                        }
                    }

                    if (operation.Responses is null)
                    {
                        continue;
                    }

                    foreach (var response in operation.Responses.Values)
                    {
                        if (response.Content is null)
                        {
                            continue;
                        }

                        foreach (var mediaType in response.Content.Values)
                        {
                            if (IsRefToBlocked(mediaType.Schema))
                            {
                                mediaType.Schema = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                };
                            }
                        }
                    }
                }
            }
        }

        if (swaggerDoc.Components?.Schemas is not null)
        {
            var toRemove = swaggerDoc
                .Components.Schemas.Where(kv => IsBlockedSchemaName(kv.Key))
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                swaggerDoc.Components.Schemas.Remove(key);
            }
        }
    }

    private static bool IsRefToBlocked(IOpenApiSchema? schema)
    {
        if (schema is null)
        {
            return false;
        }

        if (schema is OpenApiSchemaReference schemaRef)
        {
            var id = schemaRef.Reference?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                var reference = schemaRef.Reference?.ReferenceV3 ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    id = reference.Split('/')[^1];
                }
            }

            return IsBlockedSchemaName(id);
        }

        return false;
    }

    private static bool IsBlockedSchemaName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (BlockedSchemaNames.Contains(name))
        {
            return true;
        }

        // Be defensive: any obvious System.* names sneaking in as keys
        return name.StartsWith("System.", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.", StringComparison.Ordinal);
    }
}
