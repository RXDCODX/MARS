using MARS.TestKit;

namespace MARS.CinemaQueue.Tests;

public class PublicContractTests
{
    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var assemblies = new[]
        {
            "MARS.CinemaQueue.Host",
            "MARS.CinemaQueue.Domain",
            "MARS.CinemaQueue.Application",
            "MARS.CinemaQueue.Infrastructure.Postgres",
            "MARS.CinemaQueue.Infrastructure.Kinopoisk",
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
        Assert.Empty(UtcDatesConventionVerifier.Verify("MARS.CinemaQueue.Infrastructure.Postgres"));
    }
}
