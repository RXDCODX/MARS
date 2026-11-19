using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MARS.Platform.Infrastructure.Hosting.Swagger;

/// <summary>
/// Сжимает многочисленные конкретные <c>OperationResult&lt;T&gt;</c> в одну базовую
/// схему с маркером generic-параметра.
/// </summary>
/// <remarks>
/// Генератор клиента не умеет выводить generic из имени схемы, поэтому
/// конкретные <c>*OperationResult</c> остаются отдельными именами, но получают
/// структуру через <c>allOf</c> и расширение <c>x-generic-type-argument</c>.
/// </remarks>
public sealed class OperationResultSchemaFilter : IDocumentFilter
{
    private const string BaseOperationResultSchemaName = "OperationResult";

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Components?.Schemas is null)
        {
            return;
        }

        // ВАЖНО: исключаем базовый "OperationResult" без суффикса типа
        var operationResultSchemas = swaggerDoc
            .Components.Schemas.Where(kv =>
                kv.Key.EndsWith("OperationResult", StringComparison.Ordinal)
                && kv.Key != BaseOperationResultSchemaName
                && IsOperationResultSchema(kv.Key, kv.Value)
            )
            .ToList();

        if (operationResultSchemas.Count == 0)
        {
            return;
        }

        var baseOperationResultSchema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description = "Обобщенный результат операции",
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["success"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Boolean,
                    Description = "Флаг успешности операции",
                },
                ["message"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Description = "Сообщение о результате операции",
                },
                ["data"] = new OpenApiSchema
                {
                    Description = "Данные результата операции",
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        ["x-generic-type"] = new JsonValueExtension("TData"),
                    },
                },
            },
            Required = new HashSet<string> { "success" },
            AdditionalPropertiesAllowed = false,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-is-generic"] = new JsonValueExtension(true),
                ["x-generic-parameters"] = new JsonValueExtension(new JsonArray { "TData" }),
            },
        };

        swaggerDoc.Components.Schemas[BaseOperationResultSchemaName] = baseOperationResultSchema;

        foreach (var (schemaName, schema) in operationResultSchemas)
        {
            var dataProperty = schema.Properties?["data"];
            if (dataProperty is null)
            {
                continue;
            }

            var newSchema = new OpenApiSchema
            {
                AllOf =
                [
                    new OpenApiSchemaReference(BaseOperationResultSchemaName, swaggerDoc, null),
                ],
                Description =
                    schema.Description
                    ?? $"Результат операции с данными типа {GetDataTypeName(dataProperty)}",
                Extensions = new Dictionary<string, IOpenApiExtension>
                {
                    ["x-generic-type-argument"] = new JsonValueExtension(
                        GetDataTypeName(dataProperty)
                    ),
                },
            };

            swaggerDoc.Components.Schemas[schemaName] = newSchema;
        }
    }

    private static bool IsOperationResultSchema(string schemaName, IOpenApiSchema schema)
    {
        if (!schemaName.EndsWith("OperationResult", StringComparison.Ordinal))
        {
            return false;
        }

        if (schema is not OpenApiSchema concreteSchema)
        {
            return false;
        }

        if (concreteSchema.Properties is not { Count: > 0 } properties)
        {
            return false;
        }

        return properties.ContainsKey("success")
            && properties.ContainsKey("message")
            && properties.ContainsKey("data");
    }

    private static string GetDataTypeName(IOpenApiSchema dataSchema)
    {
        if (dataSchema is OpenApiSchemaReference { Id: not null } schemaRef)
        {
            return schemaRef.Id;
        }

        if (dataSchema is not OpenApiSchema concreteSchema)
        {
            return "unknown";
        }

        if (concreteSchema.AllOf?.Count > 0)
        {
            var firstRef = concreteSchema.AllOf.FirstOrDefault(s => s is OpenApiSchemaReference);
            if (firstRef is OpenApiSchemaReference { Id: not null } refSchema)
            {
                return refSchema.Id;
            }
        }

        if (concreteSchema is { Type: JsonSchemaType.Array, Items: not null })
        {
            return $"{GetDataTypeName(concreteSchema.Items)}[]";
        }

        return concreteSchema.Type.HasValue
            ? concreteSchema.Type.Value switch
            {
                JsonSchemaType.String => "string",
                JsonSchemaType.Integer => "number",
                JsonSchemaType.Number => "number",
                JsonSchemaType.Boolean => "boolean",
                JsonSchemaType.Object => "object",
                JsonSchemaType.Array => "array",
                JsonSchemaType.Null => "null",
                _ => "any",
            }
            : "any";
    }
}
