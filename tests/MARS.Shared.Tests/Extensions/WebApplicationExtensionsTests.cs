using System.Linq;
using MARS.Shared.Authentication;
using MARS.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MARS.Shared.Tests.Extensions;

/// <summary>
/// <c>RequireServiceApiKey</c> помечает маршрут минимального API требованием
/// служебного API-ключа — тем же полисом, что и остальной pipeline
/// <c>AddMarsAuthentication</c>.
/// </summary>
public class WebApplicationExtensionsTests
{
    [Fact]
    public void RequireServiceApiKeyAddsPolicyMetadata()
    {
        using var app = WebApplication.CreateBuilder().Build();

        app.MapGet("/api/probe", () => "ok").RequireServiceApiKey();

        var endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app)
                .DataSources.SelectMany(ds => ds.Endpoints)
                .OfType<RouteEndpoint>()
        );
        var authorize = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());

        Assert.Equal(ServiceAuthExtensions.PolicyName, authorize.Policy);
    }
}
