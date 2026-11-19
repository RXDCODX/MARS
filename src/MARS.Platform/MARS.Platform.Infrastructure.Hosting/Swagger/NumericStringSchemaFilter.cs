using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MARS.Platform.Infrastructure.Hosting.Swagger;

/// <summary>
/// Описывает строкой те числовые свойства, которые сериализатор пишет строкой.
/// </summary>
/// <remarks>
/// <para>
/// Discord snowflake занимает 64 бита, а <c>Number</c> браузера — 53. Поэтому
/// DTO привязки каналов помечает <c>DiscordChannelId</c> атрибутом
/// <see cref="JsonNumberHandlingAttribute"/> с <c>WriteAsString</c>: на проводе
/// значение строковое. Swashbuckle выводит схему из типа C# и видел там
/// <c>integer</c>, из-за чего сгенерированный клиент требовал число и страница
/// передавала <c>number</c> — с округлением идентификатора.
/// </para>
/// <para>
/// Фильтр читает сам атрибут, а не список известных полей: следующее свойство с
/// тем же требованием получит строковую схему без правки документа руками.
/// </para>
/// </remarks>
public sealed class NumericStringSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema || concreteSchema.Properties is null)
        {
            return;
        }

        foreach (
            var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        )
        {
            var handling = property.GetCustomAttribute<JsonNumberHandlingAttribute>();
            if (handling is null || !handling.Handling.HasFlag(JsonNumberHandling.WriteAsString))
            {
                continue;
            }

            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            var name = string.IsNullOrWhiteSpace(jsonName)
                ? JsonNamingPolicy.CamelCase.ConvertName(property.Name)
                : jsonName;

            if (
                concreteSchema.Properties.TryGetValue(name, out var propertySchema)
                && propertySchema is OpenApiSchema concretePropertySchema
            )
            {
                concretePropertySchema.Type = JsonSchemaType.String;
                concretePropertySchema.Format = null;
            }
        }
    }
}
