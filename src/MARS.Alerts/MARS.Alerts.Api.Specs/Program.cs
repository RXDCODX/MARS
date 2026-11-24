using System.Text;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace MARS.Alerts.Api.Specs;

public class Program
{
    public static async Task Main(string[] args)
    {
        var outputDirectory = ResolveOutputDirectory(args);
        Directory.CreateDirectory(outputDirectory);

        var app = MARS.Alerts.Api.Program.BuildAlertsApp(args);

        var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
        var apiDocument = swaggerProvider.GetSwagger("v1");
        var hubsDocument = swaggerProvider.GetSwagger("hubs");

        await WriteSwaggerDocumentAsync(
            Path.Combine(outputDirectory, "swagger_alerts_v1.json"),
            apiDocument
        );
        await WriteSwaggerDocumentAsync(
            Path.Combine(outputDirectory, "swagger_alerts_hubs.json"),
            hubsDocument
        );

        Console.WriteLine(
            $"OpenAPI schema generated: {Path.Combine(outputDirectory, "swagger_alerts_v1.json")}"
        );
        Console.WriteLine(
            $"OpenAPI schema generated: {Path.Combine(outputDirectory, "swagger_alerts_hubs.json")}"
        );
    }

    private static string ResolveOutputDirectory(string[] args)
    {
        var index = Array.IndexOf(args, "--output");

        if (index >= 0 && index + 1 < args.Length)
        {
            return Path.GetFullPath(args[index + 1]);
        }

        // По умолчанию — каталог api фронтенда, где лежат снимки контрактов.
        var directory = AppContext.BaseDirectory;

        for (var i = 0; i < 5; i++)
        {
            directory = Path.Combine(directory, "..");
        }

        return Path.GetFullPath(Path.Combine(directory, "MARS.ClientUi", "api"));
    }

    private static async Task WriteSwaggerDocumentAsync(string filePath, OpenApiDocument document)
    {
        await using var stream = File.Create(filePath);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        var jsonWriter = new OpenApiJsonWriter(writer);

        document.SerializeAsV3(jsonWriter);
        await writer.FlushAsync();
    }
}
