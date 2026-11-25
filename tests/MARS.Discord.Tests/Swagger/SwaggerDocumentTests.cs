using MARS.Discord.Api.Controllers;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace MARS.Discord.Tests.Swagger;

/// <summary>
/// Документ <c>v1</c>: генерация не должна падать из-за коллизии schemaId
/// у типов с одинаковым коротким именем (например, <c>OperationResult</c>).
/// </summary>
public class SwaggerDocumentTests
{
    private static Microsoft.OpenApi.OpenApiDocument GenerateV1Document()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = string.Empty;
        builder.AddMarsDefaults("MARS.Discord", dbConnectionName: null);
        builder.Services.AddControllers().AddApplicationPart(typeof(InternalController).Assembly);

        using var app = builder.Build();
        app.MapControllers();

        var provider = app.Services.GetRequiredService<ISwaggerProvider>();
        return provider.GetSwagger("v1");
    }

    [Fact]
    public void V1Document_GeneratesWithoutSchemaIdConflict()
    {
        var document = GenerateV1Document();

        Assert.NotNull(document);
        Assert.NotNull(document.Components?.Schemas);
    }

    [Fact]
    public void V1Document_ContainsBothOperationResultSchemas()
    {
        var document = GenerateV1Document();

        Assert.NotNull(document.Components?.Schemas);
        Assert.Contains("OperationResult", document.Components!.Schemas!.Keys);
        Assert.Contains("MARS_Shared_Kernel_OperationResult", document.Components.Schemas.Keys);
    }

    [Fact]
    public void V1Document_SchemaIdsAreStableAcrossGenerations()
    {
        var first = GenerateV1Document();
        var second = GenerateV1Document();

        Assert.Equal(first.Components!.Schemas!.Keys, second.Components!.Schemas!.Keys);
    }
}
