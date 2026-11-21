using MARS.TestKit;

namespace MARS.OBS.Tests;

/// <summary>
/// Контракт публичной поверхности сборки: каждый публичный тип создаётся, каждое
/// читаемое и записываемое свойство возвращает положенное значение обратно, все
/// статические инициализаторы отрабатывают без исключений.
/// </summary>
public class PublicContractTests
{
    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var assemblies = new[]
        {
            "MARS.OBS.Host",
            "MARS.OBS.Application",
            "MARS.OBS.Infrastructure",
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
}
