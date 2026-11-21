using MARS.TestKit;

namespace MARS.Scoreboard.Tests;

public class PublicContractTests
{
    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var assemblies = new[]
        {
            "MARS.Scoreboard.Host",
            "MARS.Scoreboard.Domain",
            "MARS.Scoreboard.Application",
            "MARS.Scoreboard.Infrastructure.Postgres",
        };
        var results = assemblies
            .Select(assembly => PublicContractVerifier.VerifyAssembly(assembly))
            .ToArray();

        var failures = results.SelectMany(result => result.Violations);
        Assert.Empty(failures);
        Assert.True(
            results.All(result => result.TypesInspected > 0),
            "Проверка не нашла ни одного типа: сборка загрузилась, но её типы не перечислены."
        );
    }

    [Fact]
    public void DatesAreConvertedToUtc()
    {
        Assert.Empty(UtcDatesConventionVerifier.Verify("MARS.Scoreboard.Infrastructure.Postgres"));
    }
}