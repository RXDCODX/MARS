using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace MARS.Platform.Infrastructure.Hosting.Swagger;

/// <summary>
/// Записывает произвольное JSON-значение в расширение OpenAPI.
/// </summary>
internal sealed class JsonValueExtension : IOpenApiExtension
{
    private readonly JsonNode? _value;

    public JsonValueExtension(string value) => _value = JsonNode.Parse($"\"{value}\"");

    public JsonValueExtension(bool value) => _value = JsonNode.Parse(value.ToString().ToLower());

    public JsonValueExtension(JsonNode value) => _value = value;

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        if (_value is not null)
        {
            writer.WriteRaw(_value.ToJsonString());
        }
    }
}
