using System.Collections.Generic;
using MARS.TestKit;

namespace MARS.Shared.Tests;

/// <summary>
/// Контракт публичной поверхности сборки: каждый публичный тип создаётся, каждое
/// читаемое и записываемое свойство возвращает положенное значение обратно, все
/// статические инициализаторы отрабатывают без исключений.
/// </summary>
public class PublicContractTests
{
    private static readonly string[] Assemblies = new[]
    {
        "MARS.Shared.Kernel",
        "MARS.Shared.Contracts",
        "MARS.Platform.Infrastructure.Messaging.RabbitMq",
        "MARS.Platform.Infrastructure.Persistence.Postgres",
        "MARS.Platform.Infrastructure.Grpc",
        "MARS.Platform.Infrastructure.Observability.Logging",
        "MARS.Platform.Infrastructure.Observability.Telemetry",
        "MARS.Platform.Infrastructure.Web.AspNetCore",
        "MARS.Platform.Infrastructure.Development",
        "MARS.Platform.Infrastructure.Hosting",
        "MARS.Platform.Infrastructure.Clients.Http",
    };

    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var violations = new List<string>();
        var inspected = 0;

        foreach (var assembly in Assemblies)
        {
            var result = PublicContractVerifier.VerifyAssembly(assembly);
            violations.AddRange(result.Violations.Select(v => v.ToString()));
            inspected += result.TypesInspected;
        }

        Assert.Empty(violations);
        Assert.True(
            inspected > 0,
            "Проверка не нашла ни одного типа: сборки загрузились, но их типы не перечислены."
        );
    }
}
