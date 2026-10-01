using MARS.Gateway.Swagger;
using MARS.Shared.Configuration;

namespace MARS.Gateway.Tests;

public class SwaggerEndpointMapTests
{
    [Fact]
    public void Build_ContainsObs()
    {
        // Регрессия аудита: захардкоженная карта не содержала obs.
        var map = SwaggerEndpointMap.Build(new ServiceEndpoints());

        Assert.Contains("OBS", map.Keys);
    }

    [Fact]
    public void Build_ContainsAllConfiguredServices()
    {
        var map = SwaggerEndpointMap.Build(new ServiceEndpoints());

        var expected = new[]
        {
            "TwitchCore",
            "WaifuGacha",
            "Telegram",
            "Discord",
            "Commands",
            "SoundRequest",
            "TTS",
            "OBS",
            "Alerts",
            "Scoreboard",
            "CinemaQueue",
            "MediaStorage",
            "Admin",
        };

        foreach (var name in expected)
        {
            Assert.Contains(name, map.Keys);
        }
    }

    [Fact]
    public void Build_UsesSwaggerSpecPath()
    {
        var map = SwaggerEndpointMap.Build(new ServiceEndpoints());

        foreach (var url in map.Values)
        {
            Assert.EndsWith("/swagger/v1/swagger.json", url);
        }
    }

    [Fact]
    public void Build_DoesNotDuplicateTrailingSlash()
    {
        var endpoints = new ServiceEndpoints { OBS = "http://obs:8080/" };

        var map = SwaggerEndpointMap.Build(endpoints);

        Assert.Equal("http://obs:8080/swagger/v1/swagger.json", map["OBS"]);
    }

    [Fact]
    public void Build_SkipsBlankEndpoints()
    {
        var endpoints = new ServiceEndpoints { TTS = string.Empty };

        var map = SwaggerEndpointMap.Build(endpoints);

        Assert.DoesNotContain("TTS", map.Keys);
    }
}
