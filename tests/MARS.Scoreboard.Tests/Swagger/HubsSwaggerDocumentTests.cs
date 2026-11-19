using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Scoreboard.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace MARS.Scoreboard.Tests.Swagger;

/// <summary>
/// Документ <c>hubs</c> — вход генератора SignalR-клиентов, поэтому его форма
/// проверяется здесь, а не только на живом стенде.
/// </summary>
public class HubsSwaggerDocumentTests
{
    private static Microsoft.OpenApi.OpenApiDocument GenerateHubsDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddEndpointsApiExplorer();
        builder.AddMarsHubsSwagger("MARS.Scoreboard", typeof(ScoreboardHub).Assembly);

        using var app = builder.Build();

        var provider = app.Services.GetRequiredService<ISwaggerProvider>();
        return provider.GetSwagger("hubs");
    }

    [Fact]
    public void HubsDocument_ContainsScoreboardPaths()
    {
        var document = GenerateHubsDocument();

        Assert.Contains("/hubs/scoreboard/UpdateState", document.Paths.Keys);
        Assert.Contains("/hubs/scoreboard/SetVisibility", document.Paths.Keys);
    }

    [Fact]
    public void HubsDocument_ContainsScoreboardSnapshotSchema()
    {
        var document = GenerateHubsDocument();

        Assert.NotNull(document.Components?.Schemas);
        Assert.Contains("ScoreboardSnapshot", document.Components?.Schemas!.Keys ?? []);
    }

    [Fact]
    public void HubsDocument_DoesNotContainOtherDocumentsPaths()
    {
        var document = GenerateHubsDocument();

        Assert.All(
            document.Paths.Keys,
            path => Assert.StartsWith("/hubs/", path, StringComparison.OrdinalIgnoreCase)
        );
    }
}
