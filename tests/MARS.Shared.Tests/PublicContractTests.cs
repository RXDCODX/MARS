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
        "MARS.Platform.Messaging.RabbitMq.Infrastructure",
        "MARS.Platform.Persistence.Postgres.Infrastructure",
        "MARS.Platform.Grpc.Infrastructure",
        "MARS.Platform.Observability.Logging.Infrastructure",
        "MARS.Platform.Observability.Telemetry.Infrastructure",
        "MARS.Platform.Web.AspNetCore.Infrastructure",
        "MARS.Platform.Development.Infrastructure",
        "MARS.Platform.Hosting.Infrastructure",
        "MARS.Platform.Clients.Http.Infrastructure",
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
