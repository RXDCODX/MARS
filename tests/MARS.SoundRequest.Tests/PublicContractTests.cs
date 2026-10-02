using MARS.TestKit;

namespace MARS.SoundRequest.Tests;

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
        var result = PublicContractVerifier.VerifyAssembly("MARS.SoundRequest");

        Assert.Empty(result.Violations);
        Assert.True(
            result.TypesInspected > 0,
            "Проверка не нашла ни одного типа: сборка загрузилась, но её типы не перечислены."
        );
    }
}
