using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.SoundRequest.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace MARS.SoundRequest.Tests.Swagger;

/// <summary>
/// Документ <c>hubs</c> — вход генератора SignalR-клиентов, поэтому его форма
/// проверяется здесь, а не только на живом стенде.
/// </summary>
public class HubsSwaggerDocumentTests
{
    private static async Task<Microsoft.OpenApi.OpenApiDocument> GenerateHubsDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddEndpointsApiExplorer();
        builder.AddMarsHubsSwagger("MARS.SoundRequest", typeof(SoundRequestHub).Assembly);

        await using var app = builder.Build();

        var provider = app.Services.GetRequiredService<ISwaggerProvider>();
        return provider.GetSwagger("hubs");
    }

    [Fact]
    public async Task HubsDocument_ContainsSoundRequestPaths()
    {
        var document = await GenerateHubsDocument();

        Assert.Contains("/hubs/soundrequest/Started", document.Paths.Keys);
        Assert.Contains("/hubs/soundrequest/SkipTrack", document.Paths.Keys);
        Assert.Contains("/hubs/soundrequest/PlayPrevious", document.Paths.Keys);
    }

    [Fact]
    public async Task HubsDocument_HidesCancellationTokenParameters()
    {
        var document = await GenerateHubsDocument();

        // CancellationToken — деталь сервера, в контракте клиента её быть не
        // должно: иначе генератор добавит в метод бессмысленный параметр.
        var parameterNames = document
            .Paths.Values.SelectMany(path =>
                path.Operations is { } operations
                    ? operations.Values
                    : Enumerable.Empty<Microsoft.OpenApi.OpenApiOperation>()
            )
            .SelectMany(operation => operation.Parameters ?? [])
            .Select(parameter => parameter.Name)
            .ToList();

        Assert.DoesNotContain("cancellationToken", parameterNames);
    }

    [Fact]
    public async Task HubsDocument_ContainsHubPayloadSchemas()
    {
        var document = await GenerateHubsDocument();

        Assert.NotNull(document.Components?.Schemas);
        Assert.Contains("TrackInfoHubDto", document.Components?.Schemas!.Keys ?? []);
        Assert.Contains("PlayerStateHubDto", document.Components?.Schemas!.Keys ?? []);
    }

    [Fact]
    public async Task HubsDocument_DoesNotContainOtherDocumentsPaths()
    {
        var document = await GenerateHubsDocument();

        Assert.All(
            document.Paths.Keys,
            path => Assert.StartsWith("/hubs/", path, StringComparison.OrdinalIgnoreCase)
        );
    }
}
