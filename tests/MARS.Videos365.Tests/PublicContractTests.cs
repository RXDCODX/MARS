using MARS.TestKit;

namespace MARS.Videos365.Tests;

public class PublicContractTests
{
    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var assemblies = new[]
        {
            "MARS.Videos365.Host",
            "MARS.Videos365.Domain",
            "MARS.Videos365.Application",
            "MARS.Videos365.Infrastructure",
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
        Assert.Empty(UtcDatesConventionVerifier.Verify("MARS.Videos365.Infrastructure"));
    }
}
